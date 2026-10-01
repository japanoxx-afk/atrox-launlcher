// Simulation hooks for the supported Atrox image. Keep rendering and wall-clock
// timers out of this code: every client must process returns on the same tick.
namespace construction {
constexpr char ReplayRule[] = "ATROX-WORKER-RETURN-1";
struct Pending { DWORD building; int player, type, count; };
std::vector<Pending> pending;
template<class T> T& Field(BYTE* p, size_t offset) { return *reinterpret_cast<T*>(p + offset); }
BYTE* Entity(DWORD id) {
    auto registry = *reinterpret_cast<BYTE**>(0xb27700);
    return registry ? Field<BYTE**>(registry, 8)[id] : nullptr;
}
bool HasMarker(HANDLE stream, const char* value, size_t length) {
    LARGE_INTEGER current = {}, zero = {}, end = {};
    if (stream == INVALID_HANDLE_VALUE || !SetFilePointerEx(stream, zero, &current, FILE_CURRENT) ||
        !GetFileSizeEx(stream, &end) || end.QuadPart < static_cast<LONGLONG>(length)) return false;
    DWORD bytes = static_cast<DWORD>(std::min<LONGLONG>(128, end.QuadPart));
    end.QuadPart -= bytes;
    char marker[128] = {}; DWORD count = 0;
    const bool found = SetFilePointerEx(stream, end, nullptr, FILE_BEGIN) &&
        ReadFile(stream, marker, bytes, &count, nullptr) && count == bytes &&
        std::search(marker, marker + bytes, value, value + length) != marker + bytes;
    SetFilePointerEx(stream, current, nullptr, FILE_BEGIN);
    return found;
}
bool HasRule(HANDLE stream) { return HasMarker(stream, ReplayRule, sizeof(ReplayRule)); }
bool Enabled() {
    return *reinterpret_cast<int*>(0xb1e8e8) == -1 || HasRule(*reinterpret_cast<HANDLE*>(0xb2687c));
}
void MarkRecording(const char* marker = ReplayRule, size_t length = sizeof(ReplayRule)) {
    if (*reinterpret_cast<int*>(0xb1e8e8) != -1) return;
    HANDLE stream = *reinterpret_cast<HANDLE*>(0xb2687c);
    if (stream == INVALID_HANDLE_VALUE || HasMarker(stream, marker, length)) return;
    LARGE_INTEGER current = {}, zero = {};
    if (!SetFilePointerEx(stream, zero, &current, FILE_CURRENT)) return;
    if (SetFilePointerEx(stream, zero, nullptr, FILE_END)) replay::Write(stream, marker, static_cast<DWORD>(length));
    SetFilePointerEx(stream, current, nullptr, FILE_BEGIN);
}
void Complete(BYTE* building) {
    if (!Enabled() || !building) return;
    const DWORD vtable = Field<DWORD>(building, 0);
    if (vtable != 0x5e9dc0 && vtable != 0x5eaed0) return;
    const DWORD id = Field<DWORD>(building, 0x38);
    for (const auto& item : pending) if (item.building == id) return;
    const auto owner = Field<BYTE*>(building, 0x3ac);
    if (!owner) return;
    const int player = Field<SHORT>(owner, 4);
    if (player < 0 || player >= 8) return;
    int count = 1;
    if (vtable == 0x5e9dc0) {
        const auto helpers = Field<BYTE*>(building, 0x790);
        if (helpers) count += std::max(0, std::min(2, Field<int>(helpers, 4)));
    }
    pending.push_back({ id, player, vtable == 0x5e9dc0 ? 0x1d : 0x10, count });
}
void Tick() {
    if (pending.empty() || !Enabled()) return;
    for (auto item = pending.begin(); item != pending.end();) {
        BYTE* building = Entity(item->building);
        if (!building || Field<BYTE>(building, 0x104)) { item = pending.erase(item); continue; }
        const DWORD vt = Field<DWORD>(building, 0);
        if (vt == 0x5e9dc0 || vt == 0x5eaed0) { ++item; continue; }
        auto capsule = reinterpret_cast<BYTE***>(0xb27710)[item->player];
        if (!capsule || !*capsule) { item = pending.erase(item); continue; }
        // Same perimeter placement routine used by native production. If the
        // perimeter is blocked, retain the return and retry on a later game tick.
        auto methods = reinterpret_cast<DWORD*>(vt);
        using FindExit = int (__thiscall*)(BYTE*, POINT*, int, int);
        using Create = BYTE* (__thiscall*)(BYTE*, int, int, int, int);
        POINT point = {};
        if (!reinterpret_cast<FindExit>(methods[0x194 / 4])(building, &point, item->type, 200)) { ++item; continue; }
        BYTE* worker = reinterpret_cast<Create>(0x497730)(*capsule, 0, item->type, point.x, point.y);
        if (!worker) { ++item; continue; }
        // Native activation used after trained-unit placement.
        using Activate = int (__thiscall*)(BYTE*, int, int, int);
        reinterpret_cast<Activate>(Field<DWORD*>(worker, 0)[0xb4 / 4])(worker, 1, 1, 0);
        if (--item->count == 0) item = pending.erase(item); else ++item;
    }
}
}
extern "C" __declspec(dllexport) void __stdcall ConstructionEvent(DWORD event, BYTE* entity) {
    if (event == 0) construction::Complete(entity);
    else if (event == 1) construction::Tick();
    else if (event == 2) { construction::pending.clear(); gameplay::Reset(); }
}
