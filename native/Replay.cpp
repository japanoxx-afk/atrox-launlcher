#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <algorithm>
#include <array>
#include <string>
#include <vector>
#include <cstring>

namespace replay {
constexpr size_t Tiles = 280 * 280;
constexpr size_t FogOffset = 0x39d2c, DetectionOffset = 0x601c8;
constexpr DWORD FrameOffset = 0x91520;

// Only the drawing passes see this overlay. Restore the exact original grids
// before simulation resumes, including nested drawing calls and early returns.
struct Vision {
    unsigned depth = 0;
    BYTE* players[8] = {};
    WORD fog[8][Tiles] = {}, combinedFog[Tiles] = {};
    DWORD detection[8][Tiles] = {}, combinedDetection[Tiles] = {};
    void Begin(BYTE* const source[8], bool playback) {
        if (depth++) return;
        if (!playback) return;
        // Signed fog counters: -32768 = unexplored, 0 = explored, positive = visible.
        // OR propagates the unexplored bit from opponents and hides the entire map.
        std::fill_n(combinedFog, Tiles, static_cast<WORD>(0x8000));
        memset(combinedDetection, 0, sizeof(combinedDetection));
        for (int p = 0; p < 8; ++p) {
            players[p] = source[p];
            if (!players[p]) continue;
            memcpy(fog[p], players[p] + FogOffset, sizeof(fog[p]));
            memcpy(detection[p], players[p] + DetectionOffset, sizeof(detection[p]));
            for (size_t i = 0; i < Tiles; ++i) {
                if (static_cast<SHORT>(fog[p][i]) > static_cast<SHORT>(combinedFog[i]))
                    combinedFog[i] = fog[p][i];
                combinedDetection[i] = std::max(combinedDetection[i], detection[p][i]);
            }
        }
        for (auto player : players) if (player) {
            memcpy(player + FogOffset, combinedFog, sizeof(combinedFog));
            memcpy(player + DetectionOffset, combinedDetection, sizeof(combinedDetection));
        }
    }
    void End() {
        if (!depth || --depth) return;
        for (int p = 0; p < 8; ++p) if (players[p]) {
            memcpy(players[p] + FogOffset, fog[p], sizeof(fog[p]));
            memcpy(players[p] + DetectionOffset, detection[p], sizeof(detection[p]));
            players[p] = nullptr;
        }
    }
};

bool Write(HANDLE file, const void* bytes, DWORD size) {
    DWORD written = 0;
    return WriteFile(file, bytes, size, &written, nullptr) && written == size;
}
void Append(std::vector<BYTE>& out, const void* data, size_t size) {
    const auto bytes = static_cast<const BYTE*>(data);
    out.insert(out.end(), bytes, bytes + size);
}
// Matches CHLMReplayFile::Save, including the original player index. Changing
// that index or diplomacy in the file changes simulation ownership.
std::vector<BYTE> Header(const BYTE* config, const BYTE* version, const BYTE* network) {
    std::vector<BYTE> out;
    const char* map = reinterpret_cast<const char*>(config + 0x324);
    DWORD length = static_cast<DWORD>(strnlen_s(map, 260)) + 1;
    if (length > 260 || network[0x230] >= 8) return out;
    Append(out, version, 16);
    Append(out, network + 0x7d8 + 14, 22);
    Append(out, config + 0x108, 4);
    Append(out, config + 0x104, 4);
    Append(out, &length, 4);
    Append(out, map, length);
    Append(out, network + 0x230, 1);
    Append(out, config + 0x530, 0x8c0);
    Append(out, config + 0xdf0, 8);
    return out;
}
// Deliberately narrow namespace: manually named replays are never retained/deleted.
bool ManagedName(const wchar_t* name) {
    const std::wstring n(name);
    // AtroxAuto-YYYYMMDD-HHMMSS-mmm-NNN.rec
    if (n.size() != 37 || n.compare(0, 10, L"AtroxAuto-") || n.substr(33) != L".rec") return false;
    for (size_t i = 10; i < 33; ++i) {
        if (i == 18 || i == 25 || i == 29) { if (n[i] != L'-') return false; }
        else if (n[i] < L'0' || n[i] > L'9') return false;
    }
    return true;
}
void Prune(const std::wstring& directory, unsigned maximum) {
    struct Entry { std::wstring name; ULONGLONG time; };
    std::vector<Entry> files;
    WIN32_FIND_DATAW data;
    HANDLE search = FindFirstFileW((directory + L"AtroxAuto-*.rec").c_str(), &data);
    if (search == INVALID_HANDLE_VALUE) return;
    do {
        if (!(data.dwFileAttributes & (FILE_ATTRIBUTE_DIRECTORY | FILE_ATTRIBUTE_REPARSE_POINT)) && ManagedName(data.cFileName))
            files.push_back({ data.cFileName, (static_cast<ULONGLONG>(data.ftLastWriteTime.dwHighDateTime) << 32) | data.ftLastWriteTime.dwLowDateTime });
    } while (FindNextFileW(search, &data));
    FindClose(search);
    std::sort(files.begin(), files.end(), [](const Entry& a, const Entry& b) { return a.time == b.time ? a.name < b.name : a.time < b.time; });
    for (size_t i = 0; files.size() > maximum && i < files.size() - maximum; ++i)
        DeleteFileW((directory + files[i].name).c_str());
}

// Called on the game thread after the engine finalizes the frame count. Restore
// its stream cursor even on failure, so the native manual save still works.
bool Save(HANDLE recording, const std::vector<BYTE>& header, const std::wstring& directory,
          unsigned maximum, std::wstring& saved) {
    if (header.empty() || recording == INVALID_HANDLE_VALUE) return false;
    LARGE_INTEGER zero = {}, position = {}, size = {};
    if (!SetFilePointerEx(recording, zero, &position, FILE_CURRENT) || !GetFileSizeEx(recording, &size) || size.QuadPart <= FrameOffset + 4) return false;
    LARGE_INTEGER framePosition; framePosition.QuadPart = FrameOffset;
    DWORD frames = 0, count = 0;
    bool valid = SetFilePointerEx(recording, framePosition, nullptr, FILE_BEGIN) && ReadFile(recording, &frames, 4, &count, nullptr) && count == 4 && frames > 0;
    SetFilePointerEx(recording, position, nullptr, FILE_BEGIN);
    if (!valid) return false;
    SYSTEMTIME now; GetLocalTime(&now);
    std::wstring target, staging;
    HANDLE output = INVALID_HANDLE_VALUE;
    for (unsigned attempt = 0; attempt < 1000; ++attempt) {
        wchar_t name[80];
        swprintf_s(name, L"AtroxAuto-%04u%02u%02u-%02u%02u%02u-%03u-%03u.rec", now.wYear, now.wMonth, now.wDay, now.wHour, now.wMinute, now.wSecond, now.wMilliseconds, attempt);
        target = directory + name;
        if (GetFileAttributesW(target.c_str()) != INVALID_FILE_ATTRIBUTES) continue;
        staging = target + L".partial";
        output = CreateFileW(staging.c_str(), GENERIC_WRITE, 0, nullptr, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, nullptr);
        if (output != INVALID_HANDLE_VALUE) break;
        if (GetLastError() != ERROR_FILE_EXISTS) return false;
    }
    if (output == INVALID_HANDLE_VALUE) return false;
    bool okay = Write(output, header.data(), static_cast<DWORD>(header.size())) && SetFilePointerEx(recording, zero, nullptr, FILE_BEGIN);
    BYTE buffer[65536];
    LONGLONG remaining = size.QuadPart;
    while (okay && remaining > 0) {
        const DWORD wanted = static_cast<DWORD>(std::min<LONGLONG>(remaining, sizeof(buffer)));
        okay = ReadFile(recording, buffer, wanted, &count, nullptr) && count == wanted && Write(output, buffer, count);
        remaining -= count;
    }
    if (!SetFilePointerEx(recording, position, nullptr, FILE_BEGIN)) okay = false;
    if (okay) okay = FlushFileBuffers(output) != FALSE;
    CloseHandle(output);
    if (okay) okay = MoveFileExW(staging.c_str(), target.c_str(), MOVEFILE_WRITE_THROUGH) != FALSE;
    if (!okay) { DeleteFileW(staging.c_str()); return false; }
    saved = target;
    Prune(directory, std::max(1u, std::min(maximum, 1000u)));
    return true;
}
}

#ifndef REPLAY_TEST
namespace gameplay { void Reset(); }
#include "Construction.h"
#include "Gameplay.h"
#include "SavedMap.h"
namespace {
replay::Vision vision;
std::wstring GameDirectory() {
    wchar_t path[MAX_PATH];
    if (!GetModuleFileNameW(nullptr, path, MAX_PATH)) return L"";
    std::wstring directory(path);
    return directory.substr(0, directory.find_last_of(L"\\/") + 1);
}
void Log(const std::wstring& root, const std::wstring& message) {
    const std::wstring text = message + L"\r\n";
    int bytes = WideCharToMultiByte(CP_UTF8, 0, text.c_str(), -1, nullptr, 0, nullptr, nullptr);
    std::vector<char> utf8(bytes);
    WideCharToMultiByte(CP_UTF8, 0, text.c_str(), -1, utf8.data(), bytes, nullptr, nullptr);
    HANDLE log = CreateFileW((root + L"AtroxReplay.log").c_str(), FILE_APPEND_DATA, FILE_SHARE_READ, nullptr, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (log != INVALID_HANDLE_VALUE) { replay::Write(log, utf8.data(), bytes - 1); CloseHandle(log); }
}
void AutoSave() {
    const BYTE* config = reinterpret_cast<BYTE*>(0xb1e4c0);
    if (*reinterpret_cast<const int*>(config + 0x428) != -1) return;
    const std::wstring root = GameDirectory(), ini = root + L"AtroxReplay.ini";
    if (!GetPrivateProfileIntW(L"Replay", L"AutoSave", 0, ini.c_str())) return;
    const char* temporary = reinterpret_cast<const char*>(config + 0x83c0);
    static std::string lastSaved;
    if (!*temporary || strnlen_s(temporary, MAX_PATH) == MAX_PATH || lastSaved == temporary) return;
    // The config begins with the active profile directory used by the native file browser.
    wchar_t profile[MAX_PATH], absolute[MAX_PATH];
    if (strnlen_s(reinterpret_cast<const char*>(config), MAX_PATH) == MAX_PATH ||
        !MultiByteToWideChar(CP_ACP, 0, reinterpret_cast<const char*>(config), -1, profile, MAX_PATH) ||
        !GetFullPathNameW(profile, MAX_PATH, absolute, nullptr)) return;
    std::wstring directory(absolute);
    if (directory.empty() || directory.back() != L'\\') directory += L'\\';
    const std::wstring users = root + L"Users\\";
    if (directory.size() <= users.size() || _wcsnicmp(directory.c_str(), users.c_str(), users.size()) != 0) {
        Log(root, L"Auto-save skipped: active profile is outside the game's Users folder."); return;
    }
    const BYTE* facade = *reinterpret_cast<BYTE* const*>(config + 0x84c8);
    if (!facade) return;
    const BYTE* network = *reinterpret_cast<BYTE* const*>(facade + 4);
    if (!network) return;
    const auto header = replay::Header(config, reinterpret_cast<const BYTE*>(0xb1de38), network);
    std::wstring saved;
    if (replay::Save(*reinterpret_cast<HANDLE const*>(config + 0x83bc), header, directory,
        GetPrivateProfileIntW(L"Replay", L"Maximum", 20, ini.c_str()), saved)) {
        lastSaved = temporary;
        Log(root, L"Saved: " + saved);
    } else Log(root, L"Auto-save failed; manual replay save remains available.");
}
}
extern "C" __declspec(dllexport) void __stdcall ReplayEvent(DWORD event) {
    if (event == 0) {
        construction::MarkRecording();
        construction::MarkRecording(gameplay::ReplayRule, sizeof(gameplay::ReplayRule));
        construction::MarkRecording(gameplay::SelectionRule, sizeof(gameplay::SelectionRule));
        construction::MarkRecording(gameplay::BuildingCostRule, sizeof(gameplay::BuildingCostRule));
        AutoSave(); return;
    }
    if (event == 2) { vision.End(); return; }
    BYTE* players[8] = {};
    const bool playback = *reinterpret_cast<int*>(0xb1e8e8) != -1;
    if (playback) for (int p = 0; p < 8; ++p) {
        auto capsule = reinterpret_cast<BYTE***>(0xb27710)[p];
        if (capsule) players[p] = *capsule;
    }
    vision.Begin(players, playback);
}
#endif
