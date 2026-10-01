// A saved game restores the map name, but not the network map descriptor.
// Rebuild only its identity before the saved-game world's initialization.
namespace savedmap {
struct Candidates {
    std::array<BYTE, 22> preferred{}, unique{};
    bool found = false, ambiguous = false, exact = false;
    void Add(const BYTE* identity) {
        if (!memcmp(preferred.data(), identity, 22)) exact = true;
        if (!found) { memcpy(unique.data(), identity, 22); found = true; }
        else if (memcmp(unique.data(), identity, 22)) ambiguous = true;
    }
    const BYTE* Chosen() const {
        return exact ? preferred.data() : found && !ambiguous ? unique.data() : nullptr;
    }
};
void Find(const std::string& root, const char* name, Candidates& result, unsigned depth = 0) {
    if (depth > 32) return;
    HANDLE file = CreateFileA((root + "\\" + name).c_str(), GENERIC_READ,
        FILE_SHARE_READ, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (file != INVALID_HANDLE_VALUE) {
        BYTE header[45] = {}; DWORD read = 0;
        if (ReadFile(file, header, sizeof(header), &read, nullptr) && read == sizeof(header) &&
            !memcmp(header, "Superion Map File 1.5\r\n", 23)) result.Add(header + 23);
        CloseHandle(file);
    }
    WIN32_FIND_DATAA data = {};
    HANDLE search = FindFirstFileA((root + "\\*").c_str(), &data);
    if (search == INVALID_HANDLE_VALUE) return;
    do {
        if ((data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) &&
            !(data.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) &&
            strcmp(data.cFileName, ".") && strcmp(data.cFileName, ".."))
            Find(root + "\\" + data.cFileName, name, result, depth + 1);
    } while (FindNextFileA(search, &data));
    FindClose(search);
}
}
#ifndef SAVED_MAP_TEST
extern "C" __declspec(dllexport) void __stdcall PrepareSavedMap() {
    BYTE* config = reinterpret_cast<BYTE*>(0xb1e4c0);
    BYTE* facade = *reinterpret_cast<BYTE**>(config + 0x84c8);
    if (!facade) return;
    BYTE* network = *reinterpret_cast<BYTE**>(facade + 4);
    if (!network) return;
    const char* name = reinterpret_cast<const char*>(config + 0x220);
    const char* root = reinterpret_cast<const char*>(0xb1e1b0);
    if (!memchr(name, 0, MAX_PATH) || !memchr(root, 0, MAX_PATH) ||
        !*name || !*root || strpbrk(name, "\\/:")) return;
    savedmap::Candidates candidates;
    memcpy(candidates.preferred.data(), network + 0x7de, 22);
    savedmap::Find(root, name, candidates);
    // Do not guess between different revisions of a map with the same name.
    if (const BYTE* identity = candidates.Chosen()) memcpy(network + 0x7de, identity, 22);
}
#endif
