// RPG tarzı diyalog kutusu: daktilo efekti, portre, son satırda seçenekler.
using System;
using System.Collections;
using Sorchemist.Game;
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
            var box = UIKit.Panel(root, "Box", Theme.Panel);
            UIKit.Place(box.rectTransform, new Vector2(0.5f, 0), new Vector2(110, 30), new Vector2(1450, 420), new Vector2(0.5f, 0));
            var outline = box.gameObject.AddComponent<Outline>();
            outline.effectColor = Theme.Magic; outline.effectDistance = new Vector2(4, -4);

            // Portre (yer tutucu: renkli daire + baş harf)
            var art = Art.Get(portraitKey);
            var portrait = UIKit.Img(box.transform, "Portrait", UIKit.Hex(colorHex), UIKit.Circle);
            UIKit.Place(portrait.rectTransform, new Vector2(0, 1), new Vector2(130, 30), new Vector2(230, 230));
            if (art != null)
            {
                // Daire maskeli portre (Art: portrait_<kimlik>)
                portrait.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                var face = Art.Image(portrait.transform, "Face", art);
                UIKit.Stretch(face.rectTransform, 6, 6, 6, 6);
                face.preserveAspect = false;
            }
            else
            {
                var ini = UIKit.Txt(portrait.transform, "I", name.Substring(0, 1), 110, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Stretch(ini.rectTransform);
            }

            var nameT = UIKit.Txt(box.transform, "Name", "<b>" + name + "</b>  <color=#A99FC9><size=30>" + role + "</size></color>",
                44, Theme.Accent, TextAnchor.MiddleLeft);
            UIKit.Place(nameT.rectTransform, new Vector2(0, 1), new Vector2(270, -50), new Vector2(1100, 80), new Vector2(0, 0.5f));

            _text = UIKit.Txt(box.transform, "Text", "", 40, Theme.Text, TextAnchor.UpperLeft);
            UIKit.Stretch(_text.rectTransform, 270, 95, 50, 150);

            _choices = UIKit.Rect("Choices", box.transform);
            _choices.anchorMin = new Vector2(0, 0); _choices.anchorMax = new Vector2(1, 0);
            _choices.pivot = new Vector2(0.5f, 0);
            _choices.offsetMin = new Vector2(270, 25); _choices.offsetMax = new Vector2(-30, 135);
            UIKit.HList(_choices, 16);

            // Kutunun herhangi bir yerine dokunmak ilerletir
            var advance = box.gameObject.AddComponent<Button>();
            advance.transition = Selectable.Transition.None;
            advance.onClick.AddListener(Next);

            ShowLine();
        }

        void ShowLine()
        {
            UIKit.Clear(_choices);
            if (_typing != null) StopCoroutine(_typing);
            _typing = StartCoroutine(Type(_lines[_index]));
            bool last = _index >= _lines.Length - 1;
            if (!last)
            {
                UIKit.Btn(_choices, "Next", "Devam  >", Theme.PanelLight, Next, 36);
            }
            else if (_choiceData == null || _choiceData.Length == 0)
            {
                UIKit.Btn(_choices, "Close", "Tamam", Theme.Magic, Close, 36);
            }
            else
            {
                foreach (var c in _choiceData)
                {
                    var choice = c;
                    UIKit.Btn(_choices, "C", choice.Label, choice.Color, () => { Close(); if (choice.Act != null) choice.Act(); }, 32);
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
                if (i % 3 == 0 && i < line.Length && line[i] != ' ') AudioFX.Play(Sfx.Click, 0.15f, 0.3f);
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
