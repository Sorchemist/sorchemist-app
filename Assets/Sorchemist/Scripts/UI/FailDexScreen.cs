// Fail-Dex: başarısızlık koleksiyonu. Her kart paylaşılabilir bir "an".
using System.Linq;
using Sorchemist.Core;
using Sorchemist.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class FailDexScreen : GameScreen
    {
        RectTransform _grid;
        Text _header;

        public override void Build()
        {
            _header = UIKit.Txt(Root, "Header", "", 44, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(_header.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(1000, 120));

            var sr = UIKit.Scroll(Root, "Scroll", true, false, out _grid);
            UIKit.Stretch((RectTransform)sr.transform, 0, 140, 0, 0);
            var g = UIKit.Grid(_grid, new Vector2(320, 300), new Vector2(18, 18), 24);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 3;
        }

        public override void Refresh()
        {
            UIKit.Clear(_grid);
            int have = FailDexDatabase.All.Count(f => State.FailDex.Contains(f.Id));
            _header.text = "Fail-Dex  " + have + " / " + FailDexDatabase.All.Count +
                           "\n<size=28><color=#A99FC9>Hata yapmak da bilimdir. Hepsini topla!</color></size>";

            foreach (var f in FailDexDatabase.All)
            {
                var entry = f;
                bool unlocked = State.FailDex.Contains(f.Id);
                var col = ResultPopup.RarityColor(f.Rarity);
                var card = UIKit.Btn(_grid, "F_" + f.Id, "", unlocked ? Theme.Panel : UIKit.Hex("#1F1936"),
                    () => { if (unlocked) Open(entry); else G.Toast("Bu başarısızlığı henüz yaşamadın. Deney yapmaya devam!"); }, 30);
                var outline = card.gameObject.AddComponent<Outline>();
                outline.effectColor = unlocked ? col : new Color(col.r, col.g, col.b, 0.3f);
                outline.effectDistance = new Vector2(4, -4);
                UIKit.LabelOf(card).text = unlocked
                    ? "<b>" + f.TitleTr + "</b>\n\n<size=24><color=#" + ColorUtility.ToHtmlStringRGB(col) + ">" + ResultPopup.RarityName(f.Rarity) + "</color></size>"
                    : "<size=64>?</size>\n<size=24><color=#" + ColorUtility.ToHtmlStringRGB(col) + ">" + ResultPopup.RarityName(f.Rarity) + "</color></size>";
            }
        }

        void Open(FailEntry f)
        {
            ResultPopup.Show(f.TitleTr, ResultPopup.RarityColor(f.Rarity), f.DescriptionTr, f.ScienceTr,
                ResultPopup.RarityName(f.Rarity) + " başarısızlık",
                "Fail-Dex koleksiyonuma '" + f.TitleTr + "' eklendi!", null);
        }
    }
}
