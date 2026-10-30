// RPG tarzı diyalog kutusu: daktilo efekti, portre, son satırda seçenekler.
using System;
using System.Collections;
using Sorchemist.Game;
using Sorchemist.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class DialogueChoice
    {
        public string Label; public Color Color; public Action Act;
        public DialogueChoice(string label, Color color, Action act) { Label = label; Color = color; Act = act; }
    }

    public class DialoguePanel : MonoBehaviour
    {
        string[] _lines;
        int _index;
        Text _text;
        RectTransform _choices;
        DialogueChoice[] _choiceData;
        Action _onFinish;
        Coroutine _typing;
        bool _typingDone;

        public static DialoguePanel Show(RectTransform overlay, string name, string role, string colorHex, string portraitKey,
            string[] lines, Action onFinish, params DialogueChoice[] choices)
        {
            var dim = UIKit.Img(overlay, "Dialogue", new Color(0, 0, 0, 0.45f));
            UIKit.Stretch(dim.rectTransform);
            var d = dim.gameObject.AddComponent<DialoguePanel>();
            d._lines = lines; d._onFinish = onFinish; d._choiceData = choices;
            d.Build(dim.rectTransform, name, role, colorHex, portraitKey);
            return d;
        }

        void Build(RectTransform root, string name, string role, string colorHex, string portraitKey)
        {
            // Köylülerin adı mesleğiyle aynı; meslek satırı tekrar etmesin
            if (role == name) role = Loc.T("Fındıkdere köylüsü", "Hazelbrook villager");
            var frameArt = Art.Get("frame_dialog");
            Image box;
            RectTransform portraitArea, nameArea, textArea, choiceArea;
            int textSize = 40;
            if (frameArt != null)
            {
                // Parşömen çerçeve (Dialogs.jpg). Koordinatlar çerçeve görselinin piksellerine göre.
                var fs = new Vector2(frameArt.rect.width, frameArt.rect.height);
                box = Art.Image(root, "Box", frameArt);
                box.preserveAspect = false;
                float w = 1040, h = w * fs.y / fs.x;
                UIKit.Place(box.rectTransform, new Vector2(0.5f, 0), new Vector2(110, 10), new Vector2(w, h), new Vector2(0.5f, 0));
                portraitArea = UIKit.InFrame(UIKit.Rect("PortraitArea", box.transform), fs, 158, 145, 372, 359);
                nameArea = UIKit.InFrame(UIKit.Rect("NameArea", box.transform), fs, 440, 38, 795, 112);
                textArea = UIKit.InFrame(UIKit.Rect("TextArea", box.transform), fs, 440, 185, 1010, 600);
                choiceArea = UIKit.InFrame(UIKit.Rect("ChoiceArea", box.transform), fs, 85, 380, 390, 640);
                textSize = 34;
            }
            else
            {
                box = UIKit.Panel(root, "Box", Theme.Panel);
                UIKit.Place(box.rectTransform, new Vector2(0.5f, 0), new Vector2(110, 30), new Vector2(1450, 420), new Vector2(0.5f, 0));
                var outline = box.gameObject.AddComponent<Outline>();
                outline.effectColor = Theme.Magic; outline.effectDistance = new Vector2(4, -4);
                portraitArea = UIKit.Rect("PortraitArea", box.transform);
                UIKit.Place(portraitArea, new Vector2(0, 1), new Vector2(130, 30), new Vector2(230, 230));
                nameArea = UIKit.Rect("NameArea", box.transform);
                UIKit.Place(nameArea, new Vector2(0, 1), new Vector2(270, -50), new Vector2(1100, 80), new Vector2(0, 0.5f));
                textArea = UIKit.Rect("TextArea", box.transform);
                UIKit.Stretch(textArea, 270, 95, 50, 150);
                choiceArea = UIKit.Rect("ChoiceArea", box.transform);
                choiceArea.anchorMin = new Vector2(0, 0); choiceArea.anchorMax = new Vector2(1, 0);
                choiceArea.offsetMin = new Vector2(270, 25); choiceArea.offsetMax = new Vector2(-30, 135);
            }

            // Portre: daire maskeli (Art: portrait_<kimlik>), yoksa baş harf
            var art = Art.Get(portraitKey);
            var portrait = UIKit.Img(portraitArea, "Portrait", UIKit.Hex(colorHex), UIKit.Circle);
            UIKit.Stretch(portrait.rectTransform);
            if (art != null)
            {
                portrait.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                var face = Art.Image(portrait.transform, "Face", art);
                UIKit.Stretch(face.rectTransform, 4, 4, 4, 4);
                face.preserveAspect = false;
            }
            else
            {
                var ini = UIKit.Txt(portrait.transform, "I", name.Substring(0, 1), 110, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Stretch(ini.rectTransform);
            }

            bool parchment = frameArt != null;
            var nameT = parchment
                ? UIKit.Txt(nameArea, "Name", "<b>" + name + "</b>", 34, UIKit.WoodText, TextAnchor.MiddleCenter)
                : UIKit.Txt(nameArea, "Name", "<b>" + name + "</b>  <color=#A99FC9><size=30>" + role + "</size></color>", 44, Theme.Accent, TextAnchor.MiddleLeft);
            UIKit.Stretch(nameT.rectTransform);

            if (parchment)
            {
                var roleT = UIKit.Txt(textArea, "Role", role, 26, UIKit.Hex("#7A5A3A"), TextAnchor.UpperLeft, FontStyle.Italic);
                roleT.rectTransform.anchorMin = new Vector2(0, 1); roleT.rectTransform.anchorMax = Vector2.one;
                roleT.rectTransform.pivot = new Vector2(0.5f, 1); roleT.rectTransform.sizeDelta = new Vector2(0, 36);
                roleT.rectTransform.anchoredPosition = Vector2.zero;
            }
            _text = UIKit.Txt(textArea, "Text", "", textSize, parchment ? UIKit.WoodText : Theme.Text, TextAnchor.UpperLeft);
            UIKit.Stretch(_text.rectTransform, 0, parchment ? 44 : 0, 0, 0);

            _choices = choiceArea;
            if (parchment) { var v = UIKit.VList(_choices, 10, 0); v.childForceExpandHeight = false; }
            else UIKit.HList(_choices, 16);
            _choiceHeight = parchment ? 72 : -1;

            // Kutunun herhangi bir yerine dokunmak ilerletir
            var advance = box.gameObject.AddComponent<Button>();
            advance.transition = Selectable.Transition.None;
            advance.onClick.AddListener(Next);
            // Ekranın herhangi bir yerine (kutunun dışına da) dokunmak yazıyı anında tamamlar
            var skip = root.gameObject.AddComponent<Button>();
            skip.transition = Selectable.Transition.None;
            skip.onClick.AddListener(Next);

            ShowLine();
        }

        float _choiceHeight = -1;

        Button ChoiceBtn(string label, Color color, Action act)
        {
            var b = UIKit.Btn(_choices, "C", label, color, act, 30);
            if (_choiceHeight > 0)
            {
                UIKit.Wood(b, null, Color.Lerp(Color.white, color, 0.25f));
                UIKit.LE(b, _choiceHeight);
            }
            return b;
        }

        void ShowLine()
        {
            UIKit.Clear(_choices);
            if (_typing != null) StopCoroutine(_typing);
            _typing = StartCoroutine(Type(_lines[_index]));
            bool last = _index >= _lines.Length - 1;
            if (!last)
            {
                ChoiceBtn(Loc.T("Devam  >", "Next  >"), Theme.PanelLight, Next);
            }
            else if (_choiceData == null || _choiceData.Length == 0)
            {
                ChoiceBtn(Loc.T("Tamam", "OK"), Theme.Magic, Close);
            }
            else
            {
                foreach (var c in _choiceData)
                {
                    var choice = c;
                    ChoiceBtn(choice.Label, choice.Color, () => { Close(); if (choice.Act != null) choice.Act(); });
                }
            }
        }

        IEnumerator Type(string line)
        {
            _typingDone = false;
            _text.text = "";
            for (int i = 0; i <= line.Length; i++)
            {
                _text.text = line.Substring(0, i);
                yield return new WaitForSeconds(0.022f);
            }
            _typingDone = true;
        }

        void Next()
        {
            if (!_typingDone)
            {
                // İlk dokunuş satırı tamamlar
                if (_typing != null) StopCoroutine(_typing);
                _text.text = _lines[_index];
                _typingDone = true;
                return;
            }
            if (_index < _lines.Length - 1) { _index++; ShowLine(); }
        }

        void Close()
        {
            Destroy(gameObject);
            if (_onFinish != null) _onFinish();
        }
    }
}
