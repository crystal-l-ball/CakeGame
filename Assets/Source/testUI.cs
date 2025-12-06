using UnityEngine;

public class BonusUICenterTest : MonoBehaviour {
    void Start() {
        // Big center pop
        BonusUI.Instance?.PopSprinklesCenter("10 Sprinkles!", true);
        // Add a couple of icons
        BonusUI.Instance?.AddBonusIcon(BonusType.PerfectBlow);
        BonusUI.Instance?.AddBonusIcon(BonusType.Bestie);
    }
}
