#include "../native/GameplayRules.h"
#include "../native/BuildingCosts.h"
#include <cassert>
#include <iostream>
int main() {
    using namespace gameplay;
    std::vector<unsigned char> prices(133 * ParameterStride, 0);
    for (int type = 1; type < 133; ++type) {
        auto record = prices.data() + type * ParameterStride;
        int race = type <= 57 ? 0 : type <= 72 ? 1 : 2;
        int16_t muon = static_cast<int16_t>(type * 3), gas = 75, time = 100;
        std::memcpy(record, &type, 4); std::memcpy(record + 8, &race, 4);
        std::memcpy(record + 0x98, &time, 2);
        std::memcpy(record + 0x9a, &muon, 2); std::memcpy(record + 0x9c, &gas, 2);
    }
    auto expected = prices;
    RaiseBuildingCosts(prices.data(), false);
    assert(prices == expected); // Legacy replay economics must remain unchanged.
    for (int type = 58; type <= 85; ++type) {
        int16_t muon = static_cast<int16_t>(type * 3 + 50);
        std::memcpy(expected.data() + type * ParameterStride + 0x9a, &muon, 2);
    }
    RaiseBuildingCosts(prices.data(), true);
    assert(prices == expected); // All unit/Human costs, gas/time and other fields unchanged.
    std::cout << "28 building prices increased by 50; legacy replay and all other fields preserved.\n";
    std::vector<unsigned> ids{11, 22, 33};
    std::vector<bool> ready{true, true, true};
    unsigned previous = 0;
    for (unsigned expected : {11u,22u,33u,11u}) {
        int index = NextCaster(ids, previous, ready);
        assert(index >= 0 && ids[index] == expected); previous = ids[index];
    }
    ready[1] = false; assert(ids[NextCaster(ids, 11, ready)] == 33);
    ready[0] = ready[2] = false; assert(NextCaster(ids, 33, ready) == -1);
    assert(NextCaster({}, 0, {}) == -1);
    assert(NextCaster({44}, 11, {true}) == 0);
    Bounds b{100,200,219,319};
    for (Point target : {Point{0,250},Point{500,250},Point{160,0},Point{160,500}}) {
        auto points = Perimeter(b,target);
        assert(!points.empty());
        if (target.x == 0) assert(points[0].x == b.left);
        if (target.x == 500) assert(points[0].x == b.right);
        if (target.y == 0) assert(points[0].y == b.top);
        if (target.y == 500) assert(points[0].y == b.bottom);
        // A blocked preferred exit must still leave other sides to examine.
        bool opposite = false;
        for (auto p : points) {
            assert(p.x == b.left || p.x == b.right || p.y == b.top || p.y == b.bottom);
            if (p.x == b.left && target.x == 500) opposite = true;
        }
        if (target.x == 500) assert(opposite);
    }
    static_assert(SelectionLimit == 18);
    std::vector<unsigned> army;
    for (unsigned i = 1; i <= 140; ++i) army.push_back(i);
    auto batches = Batches(army);
    std::vector<unsigned> delivered;
    for (const auto& batch : batches) {
        assert(!batch.empty() && batch.size() <= SelectionLimit);
        delivered.insert(delivered.end(), batch.begin(), batch.end());
    }
    assert(batches.size() == 8 && delivered.size() == ExtendedSelectionLimit);
    for (unsigned i = 0; i < delivered.size(); ++i) assert(delivered[i] == i + 1);
    assert(Batches({}).empty());
    assert(Batches({1,2,3})[0] == std::vector<unsigned>({1,2,3}));
    std::vector<unsigned> packet;
    for (unsigned i = 1; i <= 18; ++i) packet.push_back(i);
    assert(SelectionKind(packet) == 0);
    packet[16] = packet[17] = packet[0]; assert(SelectionKind(packet) == 1);
    packet[16] = packet[17] = packet[1]; assert(SelectionKind(packet) == 2);
    packet.pop_back(); assert(SelectionKind(packet) == 0);
    std::cout << "Gameplay perimeter and repeated-command selection tests passed.\n";
}
