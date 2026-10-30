# Sorchemist API uçtan uca testi. Yerel PHP sunucusuna karşı çalışır:
#   php -S 127.0.0.1:8765 -t Server/api   (config.php: sqlite)   sonra   python3 Server/tests/api_test.py
import base64, gzip, json, sys, urllib.request, urllib.error

URL = sys.argv[1] if len(sys.argv) > 1 else "http://127.0.0.1:8765/api.php"
fails = 0

def call(action, token=None, **body):
    body["action"] = action
    req = urllib.request.Request(URL, data=json.dumps(body).encode(), method="POST",
                                 headers={"Content-Type": "application/json", **({"Authorization": "Bearer " + token} if token else {})})
    try:
        with urllib.request.urlopen(req) as r: return json.loads(r.read())
    except urllib.error.HTTPError as e: return json.loads(e.read())

def check(cond, name):
    global fails
    print(("  OK   " if cond else "  HATA ") + name)
    if not cond: fails += 1

def gz(obj): return base64.b64encode(gzip.compress(json.dumps(obj).encode())).decode()

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
save = gz({"Gold": 100, "CompletedQuests": ["g0001"] * 2000})
village = {"name": "Ali", "tower": 3, "buildings": [{"id": "bal", "lv": 4}], "houses": [{"id": "bal", "st": 1}]}
r = call("save_put", ta, data=save, baseRev=0, stats={"level": 7, "discoveries": 20, "faildex": 3, "quests": 40}, village=village, device="pc")
check(r["ok"] and r["rev"] == 1, "ilk kayıt rev=1 (%d bayt)" % len(save))
check(call("save_put", ta, data=save, baseRev=0)["error"] == "conflict", "eski sürümle yazma çakışma verir")
check(call("save_put", ta, data=save, baseRev=0, force=True)["rev"] == 2, "force ile üzerine yazılır")
g = call("save_get", ta)
check(json.loads(gzip.decompress(base64.b64decode(g["save"]["data"])))["Gold"] == 100 and g["save"]["rev"] == 2, "kayıt geri okunur")
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
check(v["ok"] and v["village"]["tower"] == 3 and v["stats"]["quests"] == 40, "Ayşe, Ali'nin köyünü ziyaret eder")
check(call("friend_add", ta, code=a["user"]["friendCode"])["error"] == "cannot_add_self", "kendini ekleyemez")

print("Hediye ve takas")
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
call("save_put", tb, data=save, baseRev=0, stats={"level": 3, "discoveries": 5, "faildex": 1, "quests": 10})
call("save_put", tb, data=save, baseRev=1, stats={"level": 4, "discoveries": 9, "faildex": 2, "quests": 25})
lb = call("leaderboard", ta, board="quests")
check(lb["ok"] and lb["entries"][0]["name"] == "Ayşe" and lb["entries"][0]["score"] == 15, "haftalık görev sıralaması (hafta içi artış)")
check(call("leaderboard", ta, board="level", scope="friends")["entries"][0]["name"] == "Ali", "arkadaşlar arasında seviye sıralaması")

print("Lonca")
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

print("Hesap silme")
check(call("delete_account", tc, password="yanlis1234")["error"] == "wrong_credentials", "şifresiz silme yok")
check(call("delete_account", tc, password="gizli12345")["deleted"], "hesap silindi")
check(call("me", tc)["error"] == "session_expired", "silinen hesabın oturumu kapandı")
check(call("logout", ta)["ok"] and call("me", ta)["error"] == "session_expired", "çıkış")

print("\nTÜM API TESTLERİ GEÇTİ" if fails == 0 else "\n%d TEST BAŞARISIZ" % fails)
sys.exit(1 if fails else 0)
