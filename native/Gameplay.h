#include "GameplayRules.h"
#include "BuildingCosts.h"

namespace gameplay {
using construction::Field;
constexpr char ReplayRule[] = "ATROX-GAMEPLAY-1";
constexpr char SelectionRule[] = "ATROX-SELECTION-128-1";
std::vector<unsigned> extended[8], extendedPrimary[8], visual, visualPrimary;
int visualPlayer = -1;
unsigned lastCaster[8][0x2c] = {};
bool keyHeld[2] = {};

bool Enabled() {
    return *reinterpret_cast<int*>(0xb1e8e8) == -1 ||
        construction::HasMarker(*reinterpret_cast<HANDLE*>(0xb2687c), ReplayRule, sizeof(ReplayRule));
}
void Reset() {
    memset(lastCaster, 0, sizeof(lastCaster)); memset(keyHeld, 0, sizeof(keyHeld));
    for (auto& group : extended) group.clear();
    for (auto& group : extendedPrimary) group.clear();
    visual.clear(); visualPrimary.clear(); visualPlayer = -1;
}
BYTE* Entity(unsigned id) {
    BYTE* registry = *reinterpret_cast<BYTE**>(0xb27700);
    if (!registry || !id || id > Field<unsigned>(registry, 16)) return nullptr;
    return Field<BYTE**>(registry, 8)[id];
}
template<class Result, class... Args> Result Method(BYTE* object, unsigned offset, Args... args) {
    using Function = Result (__thiscall*)(BYTE*, Args...);
    return reinterpret_cast<Function>(Field<DWORD*>(object, 0)[offset/4])(object, args...);
}
bool Worker(BYTE* unit) {
    DWORD vt = Field<DWORD>(unit, 0);
    return vt == 0x5e4ef0 || vt == 0x5e34f4 || vt == 0x5e082c;
}
bool OwnedUnit(BYTE* unit, int player) {
    if (!unit || (Field<DWORD>(unit, 4) & 0x110b) != 0x110b ||
        Field<BYTE>(unit, 0x104) || Field<BYTE>(unit, 0xb5)) return false;
    BYTE* stats = Field<BYTE*>(unit, 0x3ac);
    return stats && Field<SHORT>(stats, 4) == player;
}
bool EnergySkill(BYTE* action) {
    if (!action) return false;
    // MSVC RTTI class hierarchy: use the native energy-skill base, not an
    // opcode range that could accidentally include build/transport commands.
    DWORD vt = Field<DWORD>(action, 0);
    if (vt < 0x5d0000 || vt >= 0x5f0000) return false;
    BYTE* col = *reinterpret_cast<BYTE**>(vt - 4);
    BYTE* hierarchy = Field<BYTE*>(col, 16);
    DWORD count = Field<DWORD>(hierarchy, 8);
    auto bases = Field<BYTE**>(hierarchy, 12);
    for (DWORD i = 0; i < count && i < 32; ++i)
        if (Field<DWORD>(bases[i], 0) == 0x6666c0) return true;
    return false;
}
bool Combat(BYTE* unit) {
    if (Worker(unit)) return false;
    if (Method<int>(unit, 0x104) || Method<int>(unit, 0x11c)) return true;
    for (int command = 1; command < 0x2c; ++command)
        if (EnergySkill(Field<BYTE*>(unit, 0x12c + command * 4))) return true;
    return false;
}
bool Idle(BYTE* unit) {
    BYTE* queue = Field<BYTE*>(unit, 0x108);
    return Worker(unit) && Field<int>(unit, 0x114) == 0x2d &&
        !Field<BYTE>(unit, 0x102) && queue && Field<int>(queue, 4) == 0;
}
std::vector<unsigned> Collect(int player, bool workers) {
    std::vector<unsigned> ids;
    BYTE* registry = *reinterpret_cast<BYTE**>(0xb27700);
    if (!registry) return ids;
    unsigned end = std::min(Field<unsigned>(registry, 16), 65535u);
    for (unsigned id = 1; id <= end && ids.size() < ExtendedSelectionLimit; ++id) {
        BYTE* unit = Entity(id);
        if (OwnedUnit(unit, player) && (workers ? Idle(unit) : Combat(unit))) ids.push_back(id);
    }
    return ids;
}
std::vector<unsigned> NativeIds(BYTE* list) {
    std::vector<unsigned> ids;
    for (int i = 0; i < SelectionLimit; ++i)
        if (list[8 + i*16] == 1) ids.push_back(Field<unsigned>(list, 12 + i*16));
    return ids;
}
std::vector<unsigned> LiveIds(const std::vector<unsigned>& ids, int player) {
    std::vector<unsigned> result;
    for (unsigned id : ids) if (OwnedUnit(Entity(id), player)) result.push_back(id);
    return result;
}
void Show(const std::vector<unsigned>& ids, int player, bool selected) {
    int info[5] = {selected ? 0 : 1, player, 0, 0, 0};
    for (unsigned id : ids) if (BYTE* unit = Entity(id)) Method<void>(unit, 0xa8, info);
}
void ClearVisual() {
    if (visualPlayer >= 0) Show(visual, visualPlayer, false);
    visual.clear(); visualPrimary.clear(); visualPlayer = -1;
}
void RefreshVisual(BYTE* ui) {
    if (visual.empty()) return;
    if (Field<BYTE>(ui, 0x1059) != visualPlayer ||
        LiveIds(NativeIds(ui + 0x1b8), visualPlayer) != LiveIds(visualPrimary, visualPlayer)) {
        // Clear only overflow sprites: native selection owns the panel's sprites.
        ClearVisual();
        Show(NativeIds(ui + 0x1b8), Field<BYTE>(ui, 0x1059), true);
    }
}
void SelectionPacket(BYTE* controller, DWORD* args) {
    BYTE* playerData = Field<BYTE*>(controller, 8);
    if (!playerData) return;
    unsigned player = Field<BYTE>(playerData, 0x130f0);
    if (player >= 8) return;
    extended[player].clear();
    extendedPrimary[player].clear();
    BYTE* packet = reinterpret_cast<BYTE*>(args[1]);
    BYTE* payload = Field<BYTE*>(packet, 9);
    bool enabled = *reinterpret_cast<int*>(0xb1e8e8) == -1 ||
        construction::HasMarker(*reinterpret_cast<HANDLE*>(0xb2687c), SelectionRule, sizeof(SelectionRule));
    if (!enabled || !payload || payload[0] != SelectionLimit) return;
    std::vector<unsigned> ids;
    for (unsigned i = 0; i < payload[0]; ++i) ids.push_back(Field<WORD>(payload, 1 + i*2));
    int kind = SelectionKind(ids);
    if (kind) {
        extended[player] = Collect(player, kind == 2);
        for (unsigned i = 0; i < ExtendedPanelLimit; ++i)
            extendedPrimary[player].push_back(Field<WORD>(payload, 1 + i*2));
    }
}
void Select(BYTE* ui, bool workers) {
    // Native command staging has two slots; never overwrite a queued order.
    int slot = Field<int>(ui, 0x1160);
    if (slot < 0 || slot >= 2 || Field<int>(ui, 0x1110 + slot * 4)) return;
    int player = Field<BYTE>(ui, 0x1059);
    if (player >= 8) return;
    BYTE* registry = *reinterpret_cast<BYTE**>(0xb27700);
    if (!registry) return;
    BYTE next[0x138] = {};
    Field<float>(next, 0x12d) = 1.0f;
    using Add = bool (__thiscall*)(BYTE*, unsigned);
    auto group = Collect(player, workers);
    ClearVisual();
    bool expanded = group.size() > SelectionLimit;
    size_t panel = expanded ? ExtendedPanelLimit : SelectionLimit;
    for (size_t i = 0; i < group.size() && i < panel; ++i)
        reinterpret_cast<Add>(0x46fc80)(next, group[i]);
    using Notify = void (__thiscall*)(BYTE*, int);
    reinterpret_cast<Notify>(0x46dbc0)(ui, player); // Deselect old sprites.
    memcpy(ui + 0x1b8, next, sizeof(next));
    // Native full-selection packet, visual selection and command-panel refresh.
    // Sending it through the engine also records it in multiplayer/replays.
    using Commit = void (__thiscall*)(BYTE*);
    reinterpret_cast<Commit>(0x46dd60)(ui);
    if (expanded) {
        BYTE* staged = ui + 0x1118 + slot * 34;
        BYTE* payload = Field<BYTE*>(staged, 17);
        if (payload) {
            // Encode two repeated valid IDs, not padding (the network codec
            // drops unused padding). Native Add de-duplicates them safely.
            WORD id = Field<WORD>(payload, workers ? 3 : 1);
            payload[0] = SelectionLimit;
            Field<WORD>(payload, 33) = id; Field<WORD>(payload, 35) = id;
            visual.assign(group.begin() + panel, group.end());
            visualPrimary = NativeIds(ui + 0x1b8); visualPlayer = player;
            Show(visual, player, true);
        }
    }
}
void Keyboard(BYTE* ui) {
    RefreshVisual(ui);
    const int keys[] = {VK_F2, VK_OEM_PERIOD};
    bool pressed[2] = {};
    for (int i = 0; i < 2; ++i) {
        SHORT sample = GetAsyncKeyState(keys[i]);
        bool down = (sample & 0x8000) != 0;
        pressed[i] = (sample & 1) || (down && !keyHeld[i]); keyHeld[i] = down;
    }
    if (!pressed[0] && !pressed[1]) return;
    DWORD foreground = 0; GetWindowThreadProcessId(GetForegroundWindow(), &foreground);
    if (foreground != GetCurrentProcessId() || *reinterpret_cast<int*>(0xb1e8e8) != -1 ||
        Field<BYTE>(ui, 0x105b) || Field<BYTE>(ui, 0x105c) || Field<BYTE>(ui, 0x116c) ||
        *reinterpret_cast<BYTE*>(0x6a5678) || *reinterpret_cast<BYTE*>(0x6a5271)) return;
    if ((GetAsyncKeyState(VK_CONTROL) | GetAsyncKeyState(VK_MENU) | GetAsyncKeyState(VK_SHIFT)) & 0x8000) return;
    Select(ui, pressed[1]);
}
int Spawn(BYTE* action, DWORD* args) {
    if (!Enabled()) return 0;
    BYTE* building = Field<BYTE*>(action, 4);
    BYTE* production = Method<BYTE*>(building, 0x20c);
    if (!production || !Field<BYTE>(production, 0x40)) return 0;
    int type = static_cast<int>(args[0]);
    POINT* output = reinterpret_cast<POINT*>(args[1]);
    RECT tiles = Field<RECT>(building, 0x5c8), footprint = {};
    Bounds area{tiles.left*20, tiles.top*20, tiles.right*20+19, tiles.bottom*20+19};
    if (area.right < area.left || area.bottom < area.top || area.right-area.left > 1000 || area.bottom-area.top > 1000) return 0;
    using Footprint = int (__thiscall*)(BYTE*, int, RECT*);
    reinterpret_cast<Footprint>(0x452e80)(*reinterpret_cast<BYTE**>(0xb27734), type, &footprint);
    using Free = int (__cdecl*)(RECT*, int, int, POINT*, int, int);
    const Point rally{Field<int>(production, 0x44), Field<int>(production, 0x48)};
    int attempts = 0;
    while (attempts < 200) {
        auto points = Perimeter(area, rally);
        if (points.empty()) break;
        for (auto p : points) {
            if (++attempts > 200) return 0;
            if (reinterpret_cast<Free>(0x4d54e0)(&footprint, p.x, p.y, output, type, 0)) return 1;
        }
        area.left -= 15; area.top -= 15; area.right += 15; area.bottom += 15;
    }
    return 0; // Original native exit search remains the fallback.
}
int Cast(BYTE* controller, BYTE* packet) {
    int command = Field<SHORT>(packet, 1);
    if (command <= 0 || command >= 0x2c || !Enabled()) return 0;
    BYTE* playerData = Field<BYTE*>(controller, 8);
    if (!playerData) return 0;
    unsigned player = Field<BYTE>(playerData, 0x130f0);
    if (player >= 8) return 0;
    std::vector<unsigned> ids;
    std::vector<bool> eligible;
    bool magic = false;
    BYTE order[24] = {};
    Field<int>(order, 0) = command;
    Field<SHORT>(order, 6) = Field<SHORT>(packet, 3);
    order[8] = packet[5];
    unsigned target = Field<WORD>(packet, 7);
    bool object = !(packet[5] & 0x10) && target && Entity(target);
    Field<WORD>(order, 4) = object ? 2 : 1;
    Field<int>(order, 12) = object ? target : Field<int>(packet, 9);
    Field<int>(order, 16) = object ? 0 : Field<int>(packet, 13);
    auto selected = extended[player].empty() ? NativeIds(controller + 0x14) : extended[player];
    for (unsigned id : selected) {
        BYTE* unit = Entity(id);
        if (!OwnedUnit(unit, player)) continue;
        BYTE* action = Field<BYTE*>(unit, 0x12c + command*4);
        bool skill = EnergySkill(action); magic |= skill;
        bool ready = false;
        if (skill && !Field<BYTE>(unit, 0x102)) {
            // Energy query is side-effect free; native validation then checks
            // research/target requirements on a private copy of the order.
            using Energy = int (__thiscall*)(BYTE*, int);
            if (reinterpret_cast<Energy>(0x501590)(action, command)) {
                BYTE copy[24]; memcpy(copy, order, sizeof(copy));
                ready = Method<int>(unit, 0xa4, copy) && Method<int>(action, 0x24, copy);
            }
        }
        ids.push_back(id); eligible.push_back(ready);
    }
    if (!magic) return 0;
    int chosen = NextCaster(ids, lastCaster[player][command], eligible);
    if (chosen >= 0) {
        lastCaster[player][command] = ids[chosen];
        Method<int>(Entity(ids[chosen]), 0x15c, order);
    }
    return 1; // Even when none is eligible, do not broadcast to the whole group.
}
int Command(BYTE* controller, BYTE* packet) {
    BYTE* data = Field<BYTE*>(controller, 8);
    if (!data) return 0;
    unsigned player = Field<BYTE>(data, 0x130f0);
    if (player >= 8) return 0;
    if (!extended[player].empty() &&
        LiveIds(NativeIds(controller + 0x14), player) != LiveIds(extendedPrimary[player], player)) {
        // Native control-group recall replaces the list without a full-selection
        // packet. Never let an old expanded group receive the new group's order.
        extended[player].clear(); extendedPrimary[player].clear();
    }
    if (Cast(controller, packet)) return 1;
    if (!Enabled() || extended[player].empty()) return 0;
    std::vector<unsigned> live;
    for (unsigned id : extended[player]) if (OwnedUnit(Entity(id), player)) live.push_back(id);
    BYTE original[0x138]; memcpy(original, controller + 0x14, sizeof(original));
    using Add = bool (__thiscall*)(BYTE*, unsigned);
    using NativeCommand = void (__thiscall*)(BYTE*, BYTE*);
    for (const auto& batch : Batches(live)) {
        BYTE* list = controller + 0x14;
        memset(list, 0, sizeof(original)); Field<float>(list, 0x12d) = 1.0f;
        for (unsigned id : batch) reinterpret_cast<Add>(0x46fc80)(list, id);
        // Gateway contains the displaced native prologue, then its continuation.
        reinterpret_cast<NativeCommand>(0x5a7dc0)(controller, packet);
    }
    memcpy(controller + 0x14, original, sizeof(original));
    return 1;
}
}
extern "C" __declspec(dllexport) int __stdcall GameplayEvent(DWORD event, BYTE* context, void* argument) {
    if (event == 0) return gameplay::Spawn(context, static_cast<DWORD*>(argument));
    if (event == 1) return gameplay::Command(context, static_cast<BYTE*>(argument));
    if (event == 2) gameplay::Keyboard(context);
    if (event == 3) gameplay::SelectionPacket(context, static_cast<DWORD*>(argument));
    if (event == 4) gameplay::RaiseBuildingCosts(context + 0xe24,
        *reinterpret_cast<int*>(0xb1e8e8) == -1 || construction::HasMarker(
            *reinterpret_cast<HANDLE*>(0xb2687c), gameplay::BuildingCostRule, sizeof(gameplay::BuildingCostRule)));
    return 0;
}
