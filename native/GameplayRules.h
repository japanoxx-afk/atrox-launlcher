#pragma once
#include <algorithm>
#include <array>
#include <vector>
#include <cstdint>

namespace gameplay {
constexpr int SelectionLimit = 18;
constexpr int ExtendedSelectionLimit = 128;
constexpr int ExtendedPanelLimit = 16;
inline int SelectionKind(const std::vector<unsigned>& packet) {
    if (packet.size() != SelectionLimit || !packet[0] || !packet[1] || packet[0] == packet[1] ||
        packet[16] != packet[17]) return 0;
    // Native selection packets never contain duplicate IDs. Repeated valid IDs
    // survive the native variable-length codec and remain safe for old readers.
    return packet[16] == packet[0] ? 1 : packet[16] == packet[1] ? 2 : 0;
}
inline std::vector<std::vector<unsigned>> Batches(const std::vector<unsigned>& ids) {
    std::vector<std::vector<unsigned>> result;
    size_t end = std::min(ids.size(), size_t(ExtendedSelectionLimit));
    for (size_t start = 0; start < end; start += SelectionLimit)
        result.emplace_back(ids.begin() + start, ids.begin() + std::min(end, start + SelectionLimit));
    return result;
}
struct Point { int x, y; };
struct Bounds { int left, top, right, bottom; };
// Preserve the native 15-pixel perimeter grid, but visit the rally side first.
inline std::vector<Point> Perimeter(Bounds r, Point rally) {
    std::vector<Point> points;
    for (int x = r.left; x < r.right; x += 15) points.push_back({x, r.bottom});
    for (int y = r.bottom; y > r.top; y -= 15) points.push_back({r.right, y});
    for (int x = r.right; x > r.left; x -= 15) points.push_back({x, r.top});
    for (int y = r.top; y < r.bottom; y += 15) points.push_back({r.left, y});
    auto distance = [rally](Point p) { auto x = int64_t(p.x) - rally.x, y = int64_t(p.y) - rally.y; return x*x + y*y; };
    std::stable_sort(points.begin(), points.end(), [&](Point a, Point b) { return distance(a) < distance(b); });
    return points;
}
inline int NextCaster(const std::vector<unsigned>& ids, unsigned previous,
                      const std::vector<bool>& eligible) {
    if (ids.empty() || ids.size() != eligible.size()) return -1;
    auto found = std::find(ids.begin(), ids.end(), previous);
    size_t first = found == ids.end() ? 0 : (size_t(found - ids.begin()) + 1) % ids.size();
    for (size_t n = 0; n < ids.size(); ++n) {
        size_t i = (first + n) % ids.size();
        if (eligible[i]) return static_cast<int>(i);
    }
    return -1;
}
}
