#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#define SAVED_MAP_TEST
#include <windows.h>
#include <array>
#include <string>
#include <cstring>
#include <cassert>
#include <cstdio>
#include "../native/SavedMap.h"
int main() {
    savedmap::Candidates c;
    BYTE a[22] = {1}, b[22] = {2};
    assert(!c.Chosen()); c.Add(a); assert(c.Chosen()[0] == 1);
    c.Add(a); assert(c.Chosen()[0] == 1); // identical copies are safe
    c.Add(b); assert(!c.Chosen()); // conflicting map revisions are not
    c.preferred[0] = 2; c.Add(b); assert(c.Chosen()[0] == 2);
    char temp[MAX_PATH], root[MAX_PATH]; GetTempPathA(MAX_PATH, temp);
    GetTempFileNameA(temp, "axm", 0, root); DeleteFileA(root); CreateDirectoryA(root, nullptr);
    std::string sub = std::string(root) + "\\sub"; CreateDirectoryA(sub.c_str(), nullptr);
    std::string path = sub + "\\test.spm";
    HANDLE file = CreateFileA(path.c_str(), GENERIC_WRITE, 0, nullptr, CREATE_NEW, 0, nullptr);
    BYTE header[45] = {}; memcpy(header, "Superion Map File 1.5\r\n", 23); memcpy(header + 23, a, 22);
    DWORD written; WriteFile(file, header, 45, &written, nullptr); CloseHandle(file);
    savedmap::Candidates disk; savedmap::Find(root, "test.spm", disk);
    assert(disk.Chosen() && disk.Chosen()[0] == 1);
    savedmap::Candidates absent; savedmap::Find(root, "absent.spm", absent); assert(!absent.Chosen());
    DeleteFileA(path.c_str()); RemoveDirectoryA(sub.c_str()); RemoveDirectoryA(root);
    puts("Saved-map tests passed: recursive lookup, duplicate/revision selection and missing maps.");
}
