#define REPLAY_TEST
#include "../native/Replay.cpp"
#include <cstdio>
#include <cstdlib>

void Check(bool passed, const char* message) {
    if (!passed) { fprintf(stderr, "FAIL: %s\n", message); exit(1); }
}
std::vector<BYTE> Read(const std::wstring& path) {
    HANDLE file = CreateFileW(path.c_str(), GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, 0, nullptr);
    Check(file != INVALID_HANDLE_VALUE, "open saved replay");
    std::vector<BYTE> result(GetFileSize(file, nullptr));
    DWORD count = 0;
    Check(ReadFile(file, result.data(), static_cast<DWORD>(result.size()), &count, nullptr) && count == result.size(), "read saved replay");
    CloseHandle(file); return result;
}
int wmain(int argc, wchar_t** argv) {
    static replay::Vision overlay;
    std::vector<BYTE> first(0xb2000), second(0xb2000);
    BYTE* players[8] = { first.data(), nullptr, second.data() };
    auto fogA = reinterpret_cast<WORD*>(first.data() + replay::FogOffset);
    auto fogB = reinterpret_cast<WORD*>(second.data() + replay::FogOffset);
    auto detectorB = reinterpret_cast<DWORD*>(second.data() + replay::DetectionOffset);
    std::fill_n(fogA, replay::Tiles, static_cast<WORD>(0x8000));
    std::fill_n(fogB, replay::Tiles, static_cast<WORD>(0x8000));
    fogA[10] = 1; fogB[20] = 2; fogB[30] = 0; detectorB[20] = 0x1001;
    auto originalA = first, originalB = second;
    overlay.Begin(players, false); Check(first == originalA && second == originalB, "live game must not change"); overlay.End();
    overlay.Begin(players, true);
    Check(fogA[20] == 2 && fogB[10] == 1 && fogA[30] == 0 && fogA[40] == 0x8000, "union preserves unseen/explored/current visibility");
    Check(reinterpret_cast<DWORD*>(first.data() + replay::DetectionOffset)[20] == 0x1001, "opponent unit visibility");
    overlay.Begin(players, true); overlay.End(); Check(fogA[20] == 2, "nested render"); overlay.End();
    Check(first == originalA && second == originalB, "all simulation memory restored byte-for-byte");
    overlay.End(); overlay.Begin(players, false); overlay.End();
    Check(first == originalA && second == originalB, "replay to live game transition");

    std::vector<BYTE> config(0x8500), network(0x1000), version(16, 1);
    strcpy_s(reinterpret_cast<char*>(config.data() + 0x324), 260, "Test.spm");
    network[0x230] = 3;
    auto header = replay::Header(config.data(), version.data(), network.data());
    Check(header.size() == 51 + 9 + 0x8c0 + 8 && header[59] == 3, "native replay header layout");
    std::vector<BYTE> body(replay::FrameOffset + 4 + 17, 0);
    body[replay::FrameOffset] = 1;
    if (argc > 1) {
        auto sample = Read(argv[1]);
        DWORD mapLength; memcpy(&mapLength, sample.data() + 46, 4);
        Check(mapLength > 0 && mapLength <= 260 && sample.size() > 51 + mapLength + 0x8c0 + 8, "reference replay header bounds");
        memcpy(version.data(), sample.data(), 16);
        memcpy(network.data() + 0x7d8 + 14, sample.data() + 16, 22);
        memcpy(config.data() + 0x108, sample.data() + 38, 4);
        memcpy(config.data() + 0x104, sample.data() + 42, 4);
        memcpy(config.data() + 0x324, sample.data() + 50, mapLength);
        network[0x230] = sample[50 + mapLength];
        memcpy(config.data() + 0x530, sample.data() + 51 + mapLength, 0x8c0);
        memcpy(config.data() + 0xdf0, sample.data() + 51 + mapLength + 0x8c0, 8);
        header = replay::Header(config.data(), version.data(), network.data());
        Check(std::equal(header.begin(), header.end(), sample.begin()), "header matches real native replay byte-for-byte");
        body.assign(sample.begin() + header.size(), sample.end());
    }
    wchar_t temp[MAX_PATH], unique[MAX_PATH];
    GetTempPathW(MAX_PATH, temp); GetTempFileNameW(temp, L"atr", 0, unique); DeleteFileW(unique);
    Check(CreateDirectoryW(unique, nullptr) != FALSE, "create isolated test directory");
    std::wstring directory = std::wstring(unique) + L"\\";
    HANDLE stream = CreateFileW((directory + L"recording.tmp").c_str(), GENERIC_READ | GENERIC_WRITE, 0, nullptr, CREATE_NEW, 0, nullptr);
    Check(replay::Write(stream, body.data(), static_cast<DWORD>(body.size())), "write recording fixture");
    LARGE_INTEGER before = {}, zero = {}, after = {};
    SetFilePointerEx(stream, zero, &before, FILE_CURRENT);
    const std::wstring manual = directory + L"MyManual.rec";
    HANDLE manualFile = CreateFileW(manual.c_str(), GENERIC_WRITE, 0, nullptr, CREATE_NEW, 0, nullptr); CloseHandle(manualFile);
    std::vector<std::wstring> saves;
    for (int i = 0; i < 5; ++i) {
        std::wstring saved;
        Check(replay::Save(stream, header, directory, 2, saved), "atomic auto-save");
        Check(replay::ManagedName(saved.substr(directory.size()).c_str()), "recognize generated name");
        auto expected = header; expected.insert(expected.end(), body.begin(), body.end());
        Check(Read(saved) == expected, "auto-save preserves all recorded bytes");
        saves.push_back(saved);
    }
    SetFilePointerEx(stream, zero, &after, FILE_CURRENT);
    Check(before.QuadPart == after.QuadPart, "recording stream cursor restored");
    Check(GetFileAttributesW(saves[0].c_str()) == INVALID_FILE_ATTRIBUTES && GetFileAttributesW(saves[2].c_str()) == INVALID_FILE_ATTRIBUTES && GetFileAttributesW(saves[3].c_str()) != INVALID_FILE_ATTRIBUTES, "keep two newest auto-saves");
    Check(GetFileAttributesW(manual.c_str()) != INVALID_FILE_ATTRIBUTES, "manual save preserved");
    Check(!replay::ManagedName(L"AtroxAuto-my-manual.rec"), "do not delete loosely matching manual names");
    std::wstring failed;
    Check(!replay::Save(stream, header, directory + L"missing\\", 1, failed), "failed save reported");
    Check(GetFileAttributesW(saves[3].c_str()) != INVALID_FILE_ATTRIBUTES, "no pruning on failure");
    CloseHandle(stream);
    for (const auto& path : saves) DeleteFileW(path.c_str());
    DeleteFileW(manual.c_str()); DeleteFileW((directory + L"recording.tmp").c_str()); RemoveDirectoryW(unique);
    puts("Replay tests passed: vision isolation/restoration, native format, atomic saves, retention and failures.");
}
