// Topluluk ekranı: Arkadaşlar (ziyaret, hediye, takas), Gelen Kutusu, Haftalık Sıralama ve Lonca.
// Bütün veriler sorchemist.com sunucusundan gelir (CloudService).
using System;
using System.Collections.Generic;
using System.Linq;
using Sorchemist.Core;
using Sorchemist.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class SocialScreen : GameScreen
    {
        enum Tab { Friends, Inbox, Ranking, Guild }

        Tab _tab = Tab.Friends;
        readonly Dictionary<Tab, Image> _tabButtons = new Dictionary<Tab, Image>();
        RectTransform _list, _top;
        Text _status;
        string _board = "quests", _scope = "global";
        int _requestId;

        CloudService Cloud { get { return CloudService.I; } }

        public override void Build()
        {
            var tabs = UIKit.Rect("Tabs", Root);
            tabs.anchorMin = new Vector2(0, 1); tabs.anchorMax = Vector2.one; tabs.pivot = new Vector2(0.5f, 1);
            tabs.sizeDelta = new Vector2(-32, 84); tabs.anchoredPosition = new Vector2(0, -12);
            UIKit.HList(tabs, 12);
            AddTab(tabs, Tab.Friends, Loc.T("Arkadaşlar", "Friends"));
            AddTab(tabs, Tab.Inbox, Loc.T("Gelen Kutusu", "Inbox"));
            AddTab(tabs, Tab.Ranking, Loc.T("Sıralama", "Rankings"));
            AddTab(tabs, Tab.Guild, Loc.T("Lonca", "Guild"));

            _top = UIKit.Rect("Top", Root);
            _top.anchorMin = new Vector2(0, 1); _top.anchorMax = Vector2.one; _top.pivot = new Vector2(0.5f, 1);
            _top.sizeDelta = new Vector2(-32, 90); _top.anchoredPosition = new Vector2(0, -108);

            var panel = UIKit.Panel(Root, "ListPanel", Theme.Panel);
            UIKit.Stretch(panel.rectTransform, 16, 210, 16, 10);
            RectTransform content;
            var sr = UIKit.Scroll(panel.transform, "Scroll", true, false, out content);
            UIKit.Stretch((RectTransform)sr.transform);
            UIKit.VList(content, 12, 14);
            _list = content;

            _status = UIKit.Txt(panel.transform, "Status", "", 32, Theme.TextDim);
            UIKit.Stretch(_status.rectTransform, 40, 40, 40, 40);

            Cloud.AccountChanged += () => { if (this != null && isActiveAndEnabled) Refresh(); };
        }

        void AddTab(RectTransform row, Tab t, string label)
        {
            var b = UIKit.Wood(UIKit.Btn(row, "Tab" + t, label, Theme.PanelLight, () => { _tab = t; Refresh(); }, 32));
            _tabButtons[t] = b.GetComponent<Image>();
        }

        public override void Refresh()
        {
            foreach (var kv in _tabButtons) kv.Value.color = UIKit.TabColor(kv.Key == _tab, Theme.Magic, Theme.PanelLight);
            UIKit.Clear(_list);
            UIKit.Clear(_top);
            _status.text = "";
            if (!Cloud.LoggedIn)
            {
                _status.text = Loc.T("Arkadaşlar, hediyeler, sıralamalar ve loncalar için giriş yap.\nOyunun da buluta kaydedilir.",
                    "Log in for friends, gifts, rankings and guilds.\nYour game is saved to the cloud too.");
                var b = UIKit.Wood(UIKit.Btn(_top, "Login", Loc.T("Giriş Yap / Hesap Aç", "Log In / Sign Up"), Theme.Accent, () => AccountPanel.Show(Refresh), 32),
                    UIKit.Hex("#0E3B2C"), UIKit.Hex("#B8FFE4"));
                UIKit.Stretch((RectTransform)b.transform, 300, 0, 300, 0);
                return;
            }
            _status.text = Loc.T("Yükleniyor...", "Loading...");
            int req = ++_requestId;
            switch (_tab)
            {
                case Tab.Friends: Cloud.Call("friends", null, r => { if (req == _requestId) ShowFriends(r); }, Err); break;
                case Tab.Inbox: Cloud.Call("inbox", null, r => { if (req == _requestId) ShowInbox(r); }, Err); break;
                case Tab.Ranking:
                    Cloud.Call("leaderboard", new Dictionary<string, object> { { "board", _board }, { "scope", _scope } },
                        r => { if (req == _requestId) ShowRanking(r); }, Err);
                    break;
                case Tab.Guild: Cloud.Call("guild_info", null, r => { if (req == _requestId) ShowGuild(r); }, Err); break;
            }
        }

        void Err(string code) { _status.text = CloudService.ErrorText(code); }

        // ---------------------------------------------------------------- yardımcılar
        RectTransform Row(float height, Color? bg = null)
        {
            var p = UIKit.Panel(_list, "Row", bg ?? Theme.PanelLight);
            UIKit.LE(p, height);
            return p.rectTransform;
        }

        Text RowText(RectTransform row, string text, float rightPad)
        {
            var t = UIKit.Txt(row, "T", text, 30, Theme.Text, TextAnchor.MiddleLeft);
            UIKit.Stretch(t.rectTransform, 28, 8, rightPad, 8);
            return t;
        }

        Button RowButton(RectTransform row, string label, int slot, Color color, Action onClick)
        {
            var b = UIKit.Btn(row, label, label, color, onClick, 26);
            UIKit.Place((RectTransform)b.transform, new Vector2(1, 0.5f), new Vector2(-20 - slot * 205, 0), new Vector2(190, 74), new Vector2(1, 0.5f));
            return b;
        }

        static string PotionName(string id)
        {
            var c = CompoundDatabase.Get(id);
            return c != null ? c.NameTr : id;
        }

        static string Ago(long unix)
        {
            long s = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - unix;
            if (s < 600) return Loc.T("çevrimiçi", "online");
            if (s < 3600) return Loc.T((s / 60) + " dk önce", (s / 60) + "m ago");
            if (s < 86400) return Loc.T((s / 3600) + " sa önce", (s / 3600) + "h ago");
            return Loc.T((s / 86400) + " gün önce", (s / 86400) + "d ago");
        }

        // ---------------------------------------------------------------- Arkadaşlar
        void ShowFriends(JNode r)
        {
            _status.text = "";
            UIKit.Clear(_top);
            UIKit.HList(_top, 14);
            var code = UIKit.Txt(_top, "Code", Loc.T("Kodun: ", "Your code: ") + "<color=#FFD166><b>" + Cloud.FriendCode + "</b></color>", 30, Theme.Text);
            UIKit.LE(code, -1, -1, 1.2f);
            var input = UIKit.Input(_top, "AddCode", Loc.T("Arkadaş kodu", "Friend code"), InputField.ContentType.Alphanumeric, 30, 8);
            UIKit.LE(input, -1, -1, 1);
            var add = UIKit.Wood(UIKit.Btn(_top, "Add", Loc.T("Ekle", "Add"), Theme.Accent, () =>
            {
                string c = input.text.Trim().ToUpperInvariant();
                if (c.Length != 8) { G.Toast(Loc.T("Arkadaş kodu 8 karakterdir.", "Friend codes are 8 characters.")); return; }
                Cloud.Call("friend_add", new Dictionary<string, object> { { "code", c } }, res =>
                {
                    G.Toast(res["status"].Str == "accepted"
                        ? Loc.T("Artık arkadaşsınız: ", "You're now friends with ") + res["friend"]["name"].Str
                        : Loc.T("İstek gönderildi: ", "Request sent to ") + res["friend"]["name"].Str);
                    Refresh();
                });
            }, 30));
            UIKit.LE(add, -1, -1, 0.6f);

            var incoming = r["incoming"].Items.ToList();
            var friends = r["friends"].Items.ToList();
            var outgoing = r["outgoing"].Items.ToList();

            foreach (var f in incoming)
            {
                var row = Row(100, Theme.Magic * 0.55f + Color.black * 0.45f);
                int id = f["id"].Int;
                RowText(row, "<b>" + f["name"].Str + "</b>  " + Loc.T("seni arkadaş olarak eklemek istiyor", "wants to be your friend"), 460);
                RowButton(row, Loc.T("Kabul", "Accept"), 1, Theme.Accent, () => Cloud.Call("friend_accept", new Dictionary<string, object> { { "userId", id } }, x => Refresh()));
                RowButton(row, Loc.T("Reddet", "Decline"), 0, Theme.PanelLight, () => Cloud.Call("friend_remove", new Dictionary<string, object> { { "userId", id } }, x => Refresh()));
            }

            if (friends.Count == 0 && incoming.Count == 0)
                _status.text = Loc.T("Henüz arkadaşın yok.\nKodunu paylaş ya da bir arkadaşının kodunu yukarıya yaz.",
                    "No friends yet.\nShare your code or type a friend's code above.");

            foreach (var f in friends)
            {
                var row = Row(110);
                int id = f["id"].Int;
                string name = f["name"].Str;
                RowText(row, "<b>" + name + "</b>   " + Loc.T("Sv.", "Lv.") + f["level"].Int + "\n<size=24><color=#A99FC9>" + Ago(f["lastSeen"].Long) + "</color></size>", 650);
                RowButton(row, Loc.T("Ziyaret", "Visit"), 2, Theme.Magic, () => Visit(id));
                RowButton(row, Loc.T("Hediye", "Gift"), 1, Theme.Accent, () => SendGift(id, name));
                RowButton(row, Loc.T("Takas", "Trade"), 0, Theme.Gold * 0.8f, () => OfferTrade(id, name));
            }

            foreach (var f in outgoing)
            {
                var row = Row(90, Theme.Panel);
                int id = f["id"].Int;
                RowText(row, "<color=#A99FC9>" + f["name"].Str + " — " + Loc.T("istek bekliyor", "request pending") + "</color>", 260);
                RowButton(row, Loc.T("Geri Al", "Cancel"), 0, Theme.PanelLight, () => Cloud.Call("friend_remove", new Dictionary<string, object> { { "userId", id } }, x => Refresh()));
            }
        }

        void SendGift(int toId, string name)
        {
            PotionPicker.Show(Loc.T(name + " için hediye seç", "Choose a gift for " + name), null, true, potion =>
            {
                if (State.PotionCount(potion) <= 0) return;
                Cloud.Call("gift_send", new Dictionary<string, object> { { "toId", toId }, { "potion", potion } }, r =>
                {
                    State.AddPotion(potion, -1);
                    G.SaveNow();
                    Cloud.MarkDirty();
                    AudioFX.Play(Sfx.Chime, 0.7f);
                    G.Toast(Loc.T(PotionName(potion) + " " + name + "'a gönderildi! Bugün kalan hediye: ", PotionName(potion) + " sent to " + name + "! Gifts left today: ") + r["giftsLeftToday"].Int);
                });
            });
        }

        void OfferTrade(int toId, string name)
        {
            PotionPicker.Show(Loc.T("Takasta ne vereceksin?", "What will you give?"), null, true, give =>
            {
                PotionPicker.Show(Loc.T("Karşılığında ne istiyorsun?", "What do you want in return?"), c => c.Id != give, false, want =>
                {
                    if (State.PotionCount(give) <= 0) return;
                    Cloud.Call("trade_offer", new Dictionary<string, object> { { "toId", toId }, { "give", give }, { "want", want } }, r =>
                    {
                        State.AddPotion(give, -1);   // emanete verildi
                        G.SaveNow();
                        Cloud.MarkDirty();
                        G.Toast(Loc.T("Takas teklifi gönderildi: " + PotionName(give) + " <-> " + PotionName(want),
                            "Trade offer sent: " + PotionName(give) + " <-> " + PotionName(want)));
                    });
                });
            });
        }

        // ---------------------------------------------------------------- Köy ziyareti
        void Visit(int userId)
        {
            Cloud.Call("visit", new Dictionary<string, object> { { "userId", userId } }, r =>
            {
                var v = r["village"];
                RectTransform body;
                var modal = UIKit.Modal(G.Overlay, Loc.T(r["user"]["name"].Str + "'ın Köyü", r["user"]["name"].Str + "'s Village"), out body, 940, 1500);
                if (v.IsNull)
                {
                    var t0 = UIKit.Txt(body, "Empty", Loc.T("Bu simyacı henüz köyünü buluta kaydetmemiş.", "This alchemist hasn't saved their village to the cloud yet."), 32, Theme.TextDim);
                    UIKit.Stretch(t0.rectTransform, 20, 20, 20, 120);
                }
                else
                {
                    var head = UIKit.Txt(body, "Head",
                        Loc.T("Seviye ", "Level ") + v["level"].Int + "  •  " + Loc.T("Gün ", "Day ") + v["day"].Int + "  •  " +
                        Loc.T("Kule Sv.", "Tower Lv.") + v["tower"].Int + "  •  " + Loc.T("Keşif: ", "Discoveries: ") + v["discovered"].Int +
                        "  •  Fail-Dex: " + v["faildex"].Int, 30, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                    UIKit.Place(head.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(1400, 48));

                    RectTransform grid;
                    var sr = UIKit.Scroll(body, "Grid", true, false, out grid);
                    UIKit.Stretch((RectTransform)sr.transform, 0, 60, 0, 110);
                    var g = UIKit.Grid(grid, new Vector2(200, 230), new Vector2(14, 14), 10);
                    g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 6;

                    var states = new Dictionary<string, int>();
                    foreach (var h in v["houses"].Items) states[h["id"].Str] = h["st"].Int;
                    foreach (var b in v["buildings"].Items.OrderByDescending(x => x["lv"].Int))
                    {
                        string id = b["id"].Str;
                        var cell = UIKit.Panel(grid, "B_" + id, Theme.PanelLight).rectTransform;
                        var art = Art.First(id == BuildingDatabase.Tower ? "lab_tower" : "house_" + id, "house_default");
                        if (art != null)
                        {
                            var img = Art.Image(cell, "Art", art);
                            UIKit.Place(img.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(170, 150), new Vector2(0.5f, 1));
                            img.raycastTarget = false;
                            int st;
                            if (states.TryGetValue(id, out st)) img.color = st == (int)HouseState.Frozen ? UIKit.Hex("#BDEBFF") : st == (int)HouseState.Pink ? UIKit.Hex("#FF9EC7") : Color.white;
                        }
                        string extra = "";
                        int hs;
                        if (states.TryGetValue(id, out hs) && hs != (int)HouseState.Normal) extra = "\n<size=18><color=#FFB36B>" + HouseStateName((HouseState)hs) + "</color></size>";
                        var lbl = UIKit.Txt(cell, "L", BuildingDatabase.NameTr(id) + "\n<color=#FFD166>" + Loc.T("Sv.", "Lv.") + b["lv"].Int + "</color>" + extra, 20, Theme.Text);
                        lbl.rectTransform.anchorMin = Vector2.zero; lbl.rectTransform.anchorMax = new Vector2(1, 0);
                        lbl.rectTransform.pivot = new Vector2(0.5f, 0); lbl.rectTransform.sizeDelta = new Vector2(0, 80); lbl.rectTransform.anchoredPosition = new Vector2(0, 4);
                    }
                }
                var close = UIKit.Wood(UIKit.Btn(body, "Close", Loc.T("Köyüme Dön", "Back to My Village"), Theme.Magic, () => Destroy(modal.gameObject), 32));
                UIKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(440, 90));
            });
        }

        static string HouseStateName(HouseState s)
        {
            switch (s)
            {
                case HouseState.Happy: return Loc.T("Mutlu", "Happy");
                case HouseState.GiantPumpkins: return Loc.T("Kabak istilası", "Pumpkin invasion");
                case HouseState.Frog: return Loc.T("Kurbağa!", "Frog!");
                case HouseState.Floating: return Loc.T("Uçuyor", "Floating");
                case HouseState.Frozen: return Loc.T("Donmuş", "Frozen");
                case HouseState.Foam: return Loc.T("Köpük", "Foam");
                case HouseState.Glowing: return Loc.T("Parlıyor", "Glowing");
                case HouseState.Pink: return Loc.T("Pembe", "Pink");
                case HouseState.Fireworks: return Loc.T("Havai fişek", "Fireworks");
                default: return "";
            }
        }

        // ---------------------------------------------------------------- Gelen kutusu
        void ShowInbox(JNode r)
        {
            _status.text = "";
            var info = UIKit.Txt(_top, "Info", Loc.T("Bugün gönderebileceğin hediye: ", "Gifts you can send today: ") + r["giftsLeftToday"].Int +
                Loc.T("   •   Hediyeleri ve takasları buradan al.", "   •   Collect gifts and trades here."), 28, Theme.TextDim);
            UIKit.Stretch(info.rectTransform);

            int count = 0;
            foreach (var gft in r["gifts"].Items)
            {
                count++;
                int id = gft["id"].Int;
                var row = Row(100, Theme.Accent * 0.35f + Color.black * 0.3f);
                RowText(row, "<b>" + gft["fromName"].Str + "</b> " + Loc.T("sana hediye gönderdi: ", "sent you a gift: ") + "<color=#FFD166>" + PotionName(gft["potion"].Str) + "</color>", 240);
                RowButton(row, Loc.T("Al", "Collect"), 0, Theme.Accent, () => Cloud.Call("gift_claim", new Dictionary<string, object> { { "giftId", id } }, x =>
                {
                    Receive(x["potion"].Str);
                    Refresh();
                }));
            }

            foreach (var t in r["tradesIn"].Items)
            {
                count++;
                int id = t["id"].Int;
                string give = t["give"].Str, want = t["want"].Str;
                var row = Row(120, Theme.Gold * 0.3f + Color.black * 0.35f);
                RowText(row, "<b>" + t["otherName"].Str + "</b> " + Loc.T("takas teklif ediyor:", "offers a trade:") + "\n" +
                    Loc.T("Sana verir: ", "Gives you: ") + "<color=#7CF2C8>" + PotionName(give) + "</color>   " +
                    Loc.T("İster: ", "Wants: ") + "<color=#FFB36B>" + PotionName(want) + "</color> " +
                    Loc.T("(sende " + State.PotionCount(want) + ")", "(you have " + State.PotionCount(want) + ")"), 450);
                RowButton(row, Loc.T("Kabul", "Accept"), 1, Theme.Accent, () =>
                {
                    if (State.PotionCount(want) <= 0) { G.Toast(Loc.T("Sende " + PotionName(want) + " yok. Önce laboratuvarda yap!", "You don't have " + PotionName(want) + ". Brew it in the lab first!")); return; }
                    Cloud.Call("trade_accept", new Dictionary<string, object> { { "tradeId", id } }, x =>
                    {
                        State.AddPotion(want, -1);
                        Receive(x["receive"].Str);
                        Refresh();
                    });
                });
                RowButton(row, Loc.T("Reddet", "Decline"), 0, Theme.PanelLight, () => Cloud.Call("trade_decline", new Dictionary<string, object> { { "tradeId", id } }, x => Refresh()));
            }

            foreach (var t in r["tradesOut"].Items)
            {
                count++;
                int id = t["id"].Int;
                string status = t["status"].Str;
                var row = Row(110, Theme.Panel);
                string what = PotionName(t["give"].Str) + " <-> " + PotionName(t["want"].Str);
                if (status == "open")
                {
                    RowText(row, Loc.T("Teklifin bekliyor: ", "Your offer is pending: ") + "<b>" + t["otherName"].Str + "</b>\n<size=26>" + what + "</size>", 240);
                    RowButton(row, Loc.T("İptal", "Cancel"), 0, Theme.PanelLight, () => Cloud.Call("trade_cancel", new Dictionary<string, object> { { "tradeId", id } }, x => ClaimTrade(id)));
                }
                else
                {
                    string res = status == "accepted" ? Loc.T("kabul etti!", "accepted!") : status == "declined" ? Loc.T("reddetti.", "declined.") : Loc.T("iptal edildi.", "was cancelled.");
                    RowText(row, "<b>" + t["otherName"].Str + "</b> " + res + "\n<size=26>" + what + "</size>", 240);
                    RowButton(row, Loc.T("Topla", "Collect"), 0, Theme.Accent, () => ClaimTrade(id));
                }
            }
            if (count == 0) _status.text = Loc.T("Gelen kutun boş.", "Your inbox is empty.");
        }

        void ClaimTrade(int id)
        {
            Cloud.Call("trade_claim", new Dictionary<string, object> { { "tradeId", id } }, x => { Receive(x["potion"].Str); Refresh(); }, e => Refresh());
        }

        void Receive(string potion)
        {
            if (CompoundDatabase.Get(potion) == null) return;
            State.AddPotion(potion, 1);
            G.SaveNow();
            Cloud.MarkDirty();
            AudioFX.Play(Sfx.Collect);
            G.Toast("+1 " + PotionName(potion));
        }

        // ---------------------------------------------------------------- Sıralama
        void ShowRanking(JNode r)
        {
            _status.text = "";
            UIKit.Clear(_top);
            UIKit.HList(_top, 10);
            BoardBtn("quests", Loc.T("Görev", "Quests"));
            BoardBtn("discoveries", Loc.T("Keşif", "Discoveries"));
            BoardBtn("faildex", "Fail-Dex");
            BoardBtn("level", Loc.T("Seviye", "Level"));
            ScopeBtn("global", Loc.T("Herkes", "Everyone"));
            ScopeBtn("friends", Loc.T("Arkadaşlar", "Friends"));
            ScopeBtn("guild", Loc.T("Lonca", "Guild"));

            var head = Row(70, Theme.Panel);
            RowText(head, _board == "level" ? Loc.T("<b>En yüksek seviyeler</b>", "<b>Highest levels</b>")
                : Loc.T("<b>Bu haftanın sıralaması</b>  <size=24><color=#A99FC9>(pazartesi sıfırlanır)</color></size>",
                        "<b>This week's ranking</b>  <size=24><color=#A99FC9>(resets on Monday)</color></size>"), 20);

            int me = Cloud.UserId;
            int n = 0;
            foreach (var e in r["entries"].Items)
            {
                n++;
                bool mine = e["id"].Int == me;
                int rank = e["rank"].Int;
                string medal = rank == 1 ? "<color=#FFD166>1.</color>" : rank == 2 ? "<color=#D9D9D9>2.</color>" : rank == 3 ? "<color=#CD7F32>3.</color>" : rank + ".";
                var row = Row(80, mine ? Theme.Magic * 0.6f + Color.black * 0.4f : Theme.PanelLight);
                RowText(row, "<b>" + medal + "</b>  " + e["name"].Str + "  <size=24><color=#A99FC9>" + Loc.T("Sv.", "Lv.") + e["level"].Int + "</color></size>", 220);
                var score = UIKit.Txt(row, "Score", e["score"].Int.ToString(), 34, Theme.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
                UIKit.Place(score.rectTransform, new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(200, 60), new Vector2(1, 0.5f));
            }
            if (n == 0) _status.text = Loc.T("Bu hafta henüz kimse puan almadı. İlk sen ol!", "No scores yet this week. Be the first!");
        }

        void BoardBtn(string board, string label)
        {
            var b = UIKit.Btn(_top, "B_" + board, label, _board == board ? Theme.Magic : Theme.PanelLight, () => { _board = board; Refresh(); }, 24);
            UIKit.LE(b, -1, -1, 1);
        }

        void ScopeBtn(string scope, string label)
        {
            var b = UIKit.Btn(_top, "S_" + scope, label, _scope == scope ? Theme.Accent * 0.7f : Theme.Panel, () => { _scope = scope; Refresh(); }, 24);
            UIKit.LE(b, -1, -1, 1);
        }

        // ---------------------------------------------------------------- Lonca
        void ShowGuild(JNode r)
        {
            _status.text = "";
            var g = r["guild"];
            if (g.IsNull) { ShowNoGuild(); return; }

            UIKit.HList(_top, 14);
            var title = UIKit.Txt(_top, "Name", "<b>" + g["name"].Str + "</b>   <size=24><color=#A99FC9>" + Loc.T("Lonca kodu: ", "Guild code: ") + "</color><color=#FFD166>" + g["code"].Str + "</color></size>", 34, Theme.Text);
            UIKit.LE(title, -1, -1, 3);
            string gcode = g["code"].Str;
            var share = UIKit.Btn(_top, "Share", Loc.T("Davet Et", "Invite"), Theme.PanelLight,
                () => G.Share(Loc.T("Sorchemist'te loncama katıl! Lonca kodu: ", "Join my guild in Sorchemist! Guild code: ") + gcode), 26);
            UIKit.LE(share, -1, -1, 1);
            var leave = UIKit.Btn(_top, "Leave", Loc.T("Ayrıl", "Leave"), Theme.Danger, () =>
                ChoiceDialog.Show(Loc.T("Loncadan ayrıl", "Leave guild"), Loc.T("Loncadan ayrılmak istediğine emin misin?", "Are you sure you want to leave the guild?"),
                    Loc.T("Ayrıl", "Leave"), () => Cloud.Call("guild_leave", null, x => Refresh()), Loc.T("Vazgeç", "Cancel"), null, Theme.Danger), 26);
            UIKit.LE(leave, -1, -1, 1);

            // Haftalık ortak görev
            var q = g["quest"];
            var effect = (PotionEffect)Enum.Parse(typeof(PotionEffect), q["effect"].Str);
            int target = q["target"].Int, progress = q["progress"].Int;
            bool done = q["done"].Bool, claimed = q["claimed"].Bool;
            var qrow = Row(190, Theme.Magic * 0.45f + Color.black * 0.4f);
            RowText(qrow, Loc.T("<b>Haftalık Lonca Görevi</b>\nKöyün ortak deposuna ", "<b>Weekly Guild Quest</b>\nTogether, donate ") + target +
                Loc.T(" adet <color=#FFD166>" + CompoundDatabase.EffectNameTr(effect) + "</color> iksiri bağışlayın.", " <color=#FFD166>" + CompoundDatabase.EffectNameTr(effect) + "</color> potions to the village store.") +
                "\n<size=24><color=#A99FC9>" + Loc.T("Ödül (katkı veren herkese): ", "Reward (for every contributor): ") +
                q["rewardGold"].Int + Loc.T(" altın, ", " gold, ") + q["rewardXp"].Int + " XP, " + q["rewardKnowledge"].Int + Loc.T(" Bilgi", " Knowledge") + "</color></size>", 330);
            var barBg = UIKit.Img(qrow, "BarBg", UIKit.Hex("#1E1430"));
            UIKit.Place(barBg.rectTransform, new Vector2(0, 0), new Vector2(28, 18), new Vector2(900, 22), new Vector2(0, 0));
            var fill = UIKit.Img(barBg.transform, "Fill", Theme.Accent);
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)progress / target), 1);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var pt = UIKit.Txt(qrow, "Prog", progress + " / " + target, 26, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(pt.rectTransform, new Vector2(0, 0), new Vector2(950, 12), new Vector2(200, 34), new Vector2(0, 0));

            if (!done)
                RowButton(qrow, Loc.T("Bağışla", "Donate"), 0, Theme.Accent, () => Donate(effect));
            else if (!claimed)
                RowButton(qrow, Loc.T("Ödülü Al", "Claim"), 0, Theme.Gold, () => Cloud.Call("guild_claim", null, x =>
                {
                    State.Gold += x["gold"].Int;
                    State.Knowledge += x["knowledge"].Int;
                    G.HandleLevelUps(State.AddXp(x["xp"].Int));
                    G.SaveNow(); Cloud.MarkDirty(); G.RefreshCurrent();
                    AudioFX.Play(Sfx.Coins);
                    G.Toast(Loc.T("Lonca ödülü: +", "Guild reward: +") + x["gold"].Int + Loc.T(" altın, +", " gold, +") + x["xp"].Int + " XP, +" + x["knowledge"].Int + Loc.T(" Bilgi", " Knowledge"));
                    Refresh();
                }));
            else
            {
                var ok = UIKit.Txt(qrow, "Done", Loc.T("Ödül alındı", "Claimed"), 26, Theme.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
                UIKit.Place(ok.rectTransform, new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(260, 60), new Vector2(1, 0.5f));
            }

            // Üyeler
            int me = Cloud.UserId, owner = g["ownerId"].Int;
            foreach (var m in g["members"].Items)
            {
                int id = m["id"].Int;
                var row = Row(90, id == me ? Theme.Magic * 0.5f + Color.black * 0.4f : Theme.PanelLight);
                RowText(row, "<b>" + m["name"].Str + "</b>" + (id == owner ? Loc.T("  <color=#FFD166>(başkan)</color>", "  <color=#FFD166>(leader)</color>") : "") +
                    "   <size=24><color=#A99FC9>" + Loc.T("Sv.", "Lv.") + m["level"].Int + " • " + Ago(m["lastSeen"].Long) + "</color></size>", 420);
                var c = UIKit.Txt(row, "C", Loc.T("Katkı: ", "Donated: ") + m["contrib"].Int, 26, Theme.Gold, TextAnchor.MiddleRight);
                UIKit.Place(c.rectTransform, new Vector2(1, 0.5f), new Vector2(id == me ? -30 : -240, 0), new Vector2(200, 50), new Vector2(1, 0.5f));
                if (id != me) RowButton(row, Loc.T("Ziyaret", "Visit"), 0, Theme.Magic, () => Visit(id));
            }
        }

        void Donate(PotionEffect effect)
        {
            PotionPicker.Show(Loc.T("Hangi iksiri bağışlayacaksın?", "Which potion will you donate?"), c => c.Effect == effect, true, potion =>
            {
                int have = State.PotionCount(potion);
                if (have <= 0) return;
                Cloud.Call("guild_contribute", new Dictionary<string, object> { { "potion", potion }, { "count", have } }, x =>
                {
                    int used = x["accepted"].Int;
                    State.AddPotion(potion, -used);
                    G.SaveNow(); Cloud.MarkDirty();
                    AudioFX.Play(Sfx.Chime);
                    G.Toast(Loc.T(used + " iksir loncaya bağışlandı!", used + " potions donated to the guild!"));
                    Refresh();
                });
            });
        }

        void ShowNoGuild()
        {
            var info = Row(140, Theme.Panel);
            RowText(info, Loc.T("<b>Loncalar</b>: köylerin birliği. Her hafta lonca üyeleri birlikte bir iksir görevi tamamlar, herkes ödül alır. Lonca üyeleri birbirlerinin köylerini ziyaret edebilir.",
                "<b>Guilds</b>: a union of villages. Every week members complete a potion quest together and everyone gets a reward. Members can visit each other's villages."), 20);

            var joinRow = Row(110, Theme.PanelLight);
            var code = UIKit.Input(joinRow, "Code", Loc.T("Lonca kodu", "Guild code"), InputField.ContentType.Alphanumeric, 30, 8);
            UIKit.Place((RectTransform)code.transform, new Vector2(0, 0.5f), new Vector2(28, 0), new Vector2(500, 76), new Vector2(0, 0.5f));
            RowButton(joinRow, Loc.T("Katıl", "Join"), 0, Theme.Accent, () =>
                Cloud.Call("guild_join", new Dictionary<string, object> { { "code", code.text.Trim().ToUpperInvariant() } }, x =>
                {
                    G.Toast(Loc.T("Loncaya katıldın: ", "You joined: ") + x["name"].Str);
                    Refresh();
                }));

            var createRow = Row(110, Theme.PanelLight);
            var name = UIKit.Input(createRow, "Name", Loc.T("Yeni lonca adı", "New guild name"), InputField.ContentType.Standard, 30, 20);
            UIKit.Place((RectTransform)name.transform, new Vector2(0, 0.5f), new Vector2(28, 0), new Vector2(500, 76), new Vector2(0, 0.5f));
            RowButton(createRow, Loc.T("Kur", "Create"), 0, Theme.Magic, () =>
                Cloud.Call("guild_create", new Dictionary<string, object> { { "name", name.text.Trim() } }, x =>
                {
                    G.Toast(Loc.T("Loncan kuruldu! Kod: ", "Your guild is founded! Code: ") + x["code"].Str);
                    Refresh();
                }));
        }
    }
}
