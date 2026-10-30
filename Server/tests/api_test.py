# Sorchemist API uçtan uca testi. Yerel PHP sunucusuna karşı çalışır:
#   php -S 127.0.0.1:8765 -t Server/api   (config.php: sqlite)   sonra   python3 Server/tests/api_test.py
import base64, gzip, json, sys, urllib.request, urllib.error

URL = sys.argv[1] if len(sys.argv) > 1 else "http://127.0.0.1:8765/api.php"
fails = 0
import time
NOW = int(time.time())   # sunucu saati (config'te allow_test_clock açıkken X-Test-Now ile ileri alınır)

def call(action, token=None, **body):
    body["action"] = action
    headers = {"Content-Type": "application/json", "X-Test-Now": str(NOW)}
    if token: headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(URL, data=json.dumps(body).encode(), method="POST", headers=headers)
    try:
        with urllib.request.urlopen(req) as r: return json.loads(r.read())
    except urllib.error.HTTPError as e: return json.loads(e.read())

def check(cond, name):
    global fails
    print(("  OK   " if cond else "  HATA ") + name)
    if not cond: fails += 1

def gz(obj): return base64.b64encode(gzip.compress(json.dumps(obj).encode())).decode()

EFF_POTIONS = ["water", "salt", "co2", "ammonia", "saltpeter", "bakingsoda", "peroxide", "bluevitriol", "magnesia", "quicklime",
               "coldpack", "lye", "limestone", "rust", "ethanol", "hcl", "silica"]

def state(gold=100, level=3, xp=0, quests=0, potions=None, buildings=None, ledger=0, knowledge=0, day=1, **extra):
    """Oyunun PlayerState JSON'una benzeyen kayıt."""
    s = {"Gold": gold, "Level": level, "Xp": xp, "Knowledge": knowledge, "Day": day, "MysteryOpened": 0, "LedgerId": ledger,
         "Potions": [{"Id": k, "Count": v} for k, v in (potions or {}).items()],
         "Elements": [{"Id": "H", "Count": 4}], "CompletedQuests": ["g%04d" % (i + 1) for i in range(quests)],
         "KnownElements": ["H", "O"], "Discovered": ["water"], "FailDex": ["burnt"], "OwnedPassives": [],
         "Buildings": [{"Id": k, "Level": v, "UpgradeEndsAt": 0} for k, v in (buildings or {"tower": 3, "bal": 4}).items()],
         "Houses": [{"VillagerId": "bal", "State": 1}]}
    s.update(extra)
    return s

print("Hesaplar")
a = call("register", email="ali@test.com", password="gizli12345", name="Ali")
check(a["ok"] and a["token"] and len(a["user"]["friendCode"]) == 8, "kayıt")
check(call("register", email="ali@test.com", password="gizli12345", name="Ali2")["error"] == "email_taken", "aynı e-posta reddedilir")
check(call("register", email="x@test.com", password="kisa", name="X")["error"] == "password_too_short", "kısa şifre reddedilir")
check(call("login", email="ali@test.com", password="yanlis123")["error"] == "wrong_credentials", "yanlış şifre")
la = call("login", email="ALI@test.com", password="gizli12345"); ta = la["token"]
check(la["ok"], "giriş (büyük/küçük harf duyarsız e-posta)")
b = call("register", email="ayse@test.com", password="gizli12345", name="Ayşe"); tb = b["token"]
c = call("register", email="can@test.com", password="gizli12345", name="Can"); tc = c["token"]
check(call("me", ta)["user"]["name"] == "Ali", "me")
check(call("me")["error"] == "not_logged_in" and call("me", "sahte")["error"] == "session_expired", "oturumsuz erişim yok")
check(call("google", idToken="sahte")["error"] in ("google_token_invalid", "google_wrong_audience"), "sahte Google jetonu reddedilir")

print("Bulut kaydı")
save = gz(state(quests=40, potions={p: 60 for p in ["water", "salt", "co2", "ammonia", "saltpeter", "bakingsoda", "peroxide", "bluevitriol", "magnesia", "quicklime",
                         "coldpack", "lye", "limestone", "rust", "ethanol", "hcl", "silica"]}))
r = call("save_put", ta, data=save, baseRev=0, stats={"level": 99, "quests": 99999}, device="pc")
check(r["ok"] and r["rev"] == 1, "ilk kayıt rev=1 (%d bayt)" % len(save))
check(call("save_put", ta, data=save, baseRev=0)["error"] == "conflict", "eski sürümle yazma çakışma verir")
check(call("save_put", ta, data=save, baseRev=0, force=True)["rev"] == 2, "force ile üzerine yazılır")
g = call("save_get", ta)
check(json.loads(gzip.decompress(base64.b64decode(g["save"]["data"])))["Gold"] == 100 and g["save"]["rev"] == 2, "kayıt geri okunur")
check(call("save_put", ta, data=gz({"Gold": 1, "CompletedQuests": ["g0001"] * 3}), baseRev=2)["error"] == "save_rejected", "tekrarlı görev listesi reddedilir")
check(call("save_put", ta, data="<script>", baseRev=2)["error"] == "bad_data", "geçersiz veri reddedilir")

print("Arkadaşlar")
code_b = b["user"]["friendCode"]
check(call("friend_add", ta, code=code_b)["status"] == "pending", "istek gönderildi")
check(call("visit", ta, userId=b["user"]["id"])["error"] == "not_friends", "arkadaş olmadan ziyaret yok")
fb = call("friends", tb)
check(len(fb["incoming"]) == 1, "Ayşe'ye gelen istek")
check(call("friend_accept", tb, userId=a["user"]["id"])["ok"], "kabul")
check(len(call("friends", ta)["friends"]) == 1, "artık arkadaşlar")
v = call("visit", tb, userId=a["user"]["id"])
check(v["ok"] and v["village"]["tower"] == 3 and v["stats"]["quests"] == 40, "Ayşe, Ali'nin köyünü ziyaret eder (istatistik kayıttan hesaplanır)")
check(call("friend_add", ta, code=a["user"]["friendCode"])["error"] == "cannot_add_self", "kendini ekleyemez")

print("Hediye ve takas")
call("save_put", tb, data=gz(state(quests=10, potions={"rust": 2})), baseRev=0)
check(call("gift_send", tb, toId=a["user"]["id"], potion="co2")["error"] == "not_enough_potions", "elde olmayan iksir hediye edilemez")
for i in range(5): call("gift_send", ta, toId=b["user"]["id"], potion="water")
check(call("gift_send", ta, toId=b["user"]["id"], potion="water")["error"] == "daily_gift_limit", "günde 5 hediye sınırı")
check(call("gift_send", ta, toId=c["user"]["id"], potion="water")["error"] == "not_friends", "yabancıya hediye yok")
check(call("gift_send", ta, toId=b["user"]["id"], potion="altin")["error"] == "bad_potion", "geçersiz iksir")
inbox = call("inbox", tb)
check(len(inbox["gifts"]) == 5, "Ayşe'nin gelen kutusunda 5 hediye")
gid = inbox["gifts"][0]["id"]
check(call("gift_claim", tb, giftId=gid)["potion"] == "water", "hediye alındı")
check(call("gift_claim", tb, giftId=gid)["error"] == "already_claimed", "aynı hediye iki kez alınamaz")
t = call("trade_offer", ta, toId=b["user"]["id"], give="salt", want="rust")
check(t["ok"], "takas teklifi")
check(call("trade_claim", ta, tradeId=t["tradeId"])["error"] == "trade_not_resolved", "sonuçlanmadan toplanamaz")
check(call("trade_accept", tb, tradeId=t["tradeId"])["receive"] == "salt", "Ayşe kabul eder, tuzu alır")
check(call("trade_accept", tb, tradeId=t["tradeId"])["error"] == "trade_not_open", "iki kez kabul edilemez")
check(call("trade_claim", ta, tradeId=t["tradeId"])["potion"] == "rust", "Ali pası toplar")
t2 = call("trade_offer", ta, toId=b["user"]["id"], give="lye", want="hcl")
call("trade_decline", tb, tradeId=t2["tradeId"])
check(call("trade_claim", ta, tradeId=t2["tradeId"])["potion"] == "lye", "reddedilen takasta iksir geri gelir")

print("Sıralama")
NOW += 300
call("save_put", tb, data=gz(state(quests=25, gold=400, potions={"rust": 2})), baseRev=1)
lb = call("leaderboard", ta, board="quests")
check(lb["ok"] and lb["entries"][0]["name"] == "Ayşe" and lb["entries"][0]["score"] == 15, "haftalık görev sıralaması (hafta içi artış)")
check(call("leaderboard", ta, board="level", scope="friends")["entries"][0]["name"] == "Ali", "arkadaşlar arasında seviye sıralaması")

print("Lonca")
call("save_put", tc, data=gz(state(potions={p: 20 for p in EFF_POTIONS})), baseRev=0)
gc = call("guild_create", ta, name="Fındık Simyacıları")
check(gc["ok"] and len(gc["code"]) == 8, "lonca kuruldu")
check(call("guild_join", tc, code=gc["code"])["ok"], "Can katıldı")
gi = call("guild_info", tc)["guild"]
check(len(gi["members"]) == 2 and gi["quest"]["target"] == 20, "üyeler ve haftalık görev (hedef 20)")
eff = gi["quest"]["effect"]
EFF = {"Hydrate": "water", "Preserve": "salt", "Fizz": "co2", "Growth": "ammonia", "Fireworks": "saltpeter", "Soothe": "bakingsoda",
       "Disinfect": "peroxide", "PestControl": "bluevitriol", "Light": "magnesia", "Warmth": "quicklime", "Cool": "coldpack",
       "Cleanse": "lye", "Build": "limestone", "Pigment": "rust", "Fuel": "ethanol", "Dissolve": "hcl", "Glass": "silica"}
wrong = [p for e, p in EFF.items() if e != eff][0]
check(call("guild_contribute", tc, potion=wrong, count=1)["error"] == "wrong_effect", "yanlış etki kabul edilmez")
check(call("guild_claim", tc)["error"] == "quest_not_done", "bitmeden ödül yok")
call("guild_contribute", tc, potion=EFF[eff], count=12)
r = call("guild_contribute", ta, potion=EFF[eff], count=50)
check(r["accepted"] == 8 and r["quest"]["done"], "hedefi aşan bağış kırpılır, görev tamam")
check(call("guild_claim", tc)["gold"] == 150, "Can ödülünü aldı")
check(call("guild_claim", tc)["error"] == "nothing_to_claim", "ödül bir kez alınır")
check(call("visit", tc, userId=a["user"]["id"])["ok"], "lonca üyesi köyü ziyaret edebilir")
call("guild_leave", ta)
check(call("guild_info", tc)["guild"]["ownerId"] == c["user"]["id"], "başkan ayrılınca başkanlık devredilir")

print("Hile koruması")
POT = {p: 60 for p in EFF_POTIONS}
def put(tok, rev, **kw):
    return call("save_put", tok, data=gz(state(**kw)), baseRev=rev)
rev = call("save_get", ta)["save"]["rev"]
lmax = call("me", ta)["ledgerMax"]
check(lmax > 0, "hediye/takas/lonca hareketleri deftere yazıldı (%d)" % lmax)
r = put(ta, rev, quests=40, potions=POT, ledger=lmax + 50)
check(r.get("error") == "save_rejected" and "ledger_future" in r["reasons"], "var olmayan defter numarası reddedilir")
NOW += 3 * 86400
r = put(ta, rev, quests=200, gold=60000, potions=POT, ledger=lmax, buildings={"tower": 6, "bal": 7}, level=20, knowledge=900, day=150)
check(r.get("ok") and r["rev"] == rev + 1, "3 gün dürüst oyun kabul edilir")
rev = r.get("rev", rev)
NOW += 3700
r = put(ta, rev, quests=200, gold=9000000, potions=POT, ledger=lmax, buildings={"tower": 6, "bal": 7}, level=20, knowledge=900, day=150)
check(r.get("error") == "save_rejected" and "gold_jump" in r["reasons"], "1 saatte 9 milyon altın reddedilir")
NOW += 3700
r = put(ta, rev, quests=200, gold=60000, potions=POT, ledger=lmax, buildings={"tower": 11, "bal": 7}, level=20, knowledge=900, day=150)
check(r.get("error") == "save_rejected" and "building_too_fast" in r["reasons"], "anında kule seviyesi atlama reddedilir")
NOW += 3700
r = put(ta, rev, quests=2000, gold=60000, potions=POT, ledger=lmax, buildings={"tower": 6, "bal": 7}, level=20, knowledge=900, day=150)
check(r.get("error") == "save_rejected" and "too_many_quests" in r["reasons"], "birkaç saatte 1800 görev reddedilir")
NOW += 3700
r = put(ta, rev, quests=200, gold=60000, potions={**POT, "water": 5000}, ledger=lmax, buildings={"tower": 6, "bal": 7}, level=20, knowledge=900, day=150)
check(r.get("error") == "save_rejected" and "potion_jump" in r["reasons"], "birden çoğalan iksirler reddedilir")
NOW += 3700
r = put(ta, rev, quests=200, gold=60000, potions=POT, ledger=0, buildings={"tower": 6, "bal": 7}, level=20, knowledge=900, day=150)
check(r.get("error") == "save_rejected" and "ledger_rewind" in r["reasons"], "defteri geri sarmak reddedilir")
me = call("me", ta)["user"]
check(me["flagged"], "tekrarlanan hile denemelerinden sonra hesap işaretlenir")
check(call("gift_send", ta, toId=b["user"]["id"], potion="water")["error"] == "account_flagged", "işaretli hesap hediye gönderemez")
check(all(e["name"] != "Ali" for e in call("leaderboard", tb, board="level")["entries"]), "işaretli hesap sıralamada görünmez")
check(call("save_get", ta)["save"]["rev"] == rev, "reddedilen kayıtlar buluttaki geçerli kaydı bozmaz")

print("Hesap silme")
check(call("delete_account", tc, password="yanlis1234")["error"] == "wrong_credentials", "şifresiz silme yok")
check(call("delete_account", tc, password="gizli12345")["deleted"], "hesap silindi")
check(call("me", tc)["error"] == "session_expired", "silinen hesabın oturumu kapandı")
check(call("logout", ta)["ok"] and call("me", ta)["error"] == "session_expired", "çıkış")

print("\nTÜM API TESTLERİ GEÇTİ" if fails == 0 else "\n%d TEST BAŞARISIZ" % fails)
sys.exit(1 if fails else 0)
