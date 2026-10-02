#pragma once
#include <cstdint>
#include <cstring>

namespace gameplay {
constexpr char BuildingCostRule[] = "ATROX-BUILD-COST-50-1";
constexpr unsigned ParameterStride = 0x1c0;

// The parameter table is indexed by native entity type. 58..72 are Intellian
// buildings, 73..85 Critis buildings. Workers and combat units have other IDs.
inline void RaiseBuildingCosts(unsigned char* parameters, bool enabled) {
    if (!parameters || !enabled) return;
    for (int type = 58; type <= 85; ++type) {
        auto record = parameters + type * ParameterStride;
        int id = 0, race = 0;
        int16_t muon = 0;
        std::memcpy(&id, record, sizeof(id));
        std::memcpy(&race, record + 8, sizeof(race));
        std::memcpy(&muon, record + 0x9a, sizeof(muon));
        if (id != type || race != (type <= 72 ? 1 : 2) || muon < 0 || muon > 32717) continue;
        muon = static_cast<int16_t>(muon + 50);
        std::memcpy(record + 0x9a, &muon, sizeof(muon));
    }
}
}
