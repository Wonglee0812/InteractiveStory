using UnityEngine;

public enum AIWorldState
{
    HasTarget,              // 0
    IsAlerted,              // 1
    HasSuspectedLocation,   // 2
    IsTargetInStealthGrass, // 3
    HasCombatToken,         // 4
    IsLightStaggered,       // 5: Khựng nhẹ (Vỡ Giáp)
    IsHardStaggered,        // 6: Gục ngã (Nổ Core)
    HasInvestigateToken,    // 7
    IsSearching             // 8
}