## Server Orders
custom-rules = Bu harita özel kurallar içermektedir. Oyun deneyimi değişebilir.
map-bots-disabled = Botlar bu haritada devre dışı bırakılmışdır.
two-humans-required = Bu sunucu oyunu başlatmak için en az iki insan oyuncu gerektirir.
unknown-server-command = Bilinmeyen sunucu komutu: { $command }
only-only-host-start-game = Sadece sunucu oyunu başlatabilir.
no-start-until-required-slots-full = Gerekli miktarda slot dolana kadar oyun başlatılamıyor.
no-start-without-players = Oyun hiçbir oyuncu olmadan başlatılamıyor.
insufficient-enabled-spawnPoints = Oyun daha fazla başlangıç noktası etkinleştirilene kadar başlatılamıyor.
malformed-command = Bozuk { $command } komutu
chat-disabled = Sohbet devre dışı. Lütfen { $remaining } saniye sonra tekrar deneyin.
state-unchanged-ready = Hazır olarak işaretlenmişken durum değiştirilemiyor.
invalid-faction-selected = Geçersiz taraf seçimi: { $faction }
supported-factions = Desteklenen değerler: { $factions }
state-unchanged-game-started = Oyun başladıktan sonra durum değiştirilemiyor. ({ $command })
requires-host = Sadece sunucu bunu yapabilir.
invalid-bot-slot = Başka bir istemciye sahip slota bot eklenemiyor.
invalid-bot-type = Geçersiz bot türü.
only-host-change-map = Sadece sunucu haritayı değiştirebilir.
lobby-disconnected = { $player } ayrıldı.
player-disconnected =
    { $team ->
        [0] { $player } adlı oyuncunun bağlantısı kesildi.
       *[other] { $player } (Takım { $team }) adlı oyuncunun bağlantısı kesildi.
    }
observer-disconnected = { $player } (İzleyici) adlı oyuncunun bağlantısı kesildi.
unknown-map = Harita sunucuda bulunamadı.
searching-map = Harita Kaynak Merkezinde aranıyor...
only-host-change-configuration = Sadece sunucu yapılandırmayı değiştirebilir.
changed-map = { $player } haritayı { $map } olarak değiştirdi.
value-changed = { $player }, { $name } seçeneğini { $value } olarak değiştirdi.
you-were-kicked = Sunucudan atıldınız.
kicked = { $admin }, { $player } adlı oyuncuyu sunucudan attı.
temp-ban = { $admin }, { $player } adlı oyuncuyu geçici olarak sunucudan engelledi.
only-host-transfer-admin = Sadece yöneticiler yöneticiliği başka bir oyuncuya devredebilir.
only-host-move-spectators = Sadece yöneticiler oyuncuları izleyiciliğe taşıyabilir.
empty-slot = O slotta kimse yok.
move-spectators = { $admin }, { $player } adlı oyuncuyu izleyiciliğe taşıdı.
nick = { $player } artık { $name } olarak biliniyor.
player-dropped = Bir oyuncu süre aşımından dolayı atıldı.
connection-problems = { $player } bağlantı sorunları yaşıyor.
timeout = { $player } süre aşımından dolayı atıldı.
timeout-in = { $player }, { $timeout } saniye içinde atılacak.
error-game-started = Oyun zaten başlatıldı.
requires-password = Sunucu şifre gerektiriyor.
incorrect-password = Geçersiz şifre.
incompatible-mod = Sunucu uyumsuz bir mod çalıştırıyor.
incompatible-version = Sunucu uyumsuz bir sürüm çalıştırıyor.
incompatible-protocol = Sunucu uyumsuz bir protokol çalıştırıyor.
banned = Sunucudan engellendiniz.
temp-banned = Sunucudan geçici olarak engellendiniz.
full = Oyun dolu.
joined = { $player } oyuna katıldı.
new-admin = { $player } artık yönetici.
option-locked = { $option } değiştirlemez.
invalid-configuration-command = Geçersiz yapılandırma komutu.
admin-option = Sadece sunucu o seçeneği değiştirebilir.
number-teams = Takım miktarı ayrıştırılamadı: { $raw }
admin-kick = Sadece sunucu oyuncuları atabilir.
kick-none = O slotta kimse yok.
no-kick-game-started = Sadece izleyicilier oyun başladıktan sonra atılabilir.
admin-clear-spawn = Sadece yöneticiler başlangıç noktalarını temizleyebilir.
spawn-occupied = Başka bir oyuncu ile aynı başlangıç noktasını seçemeziniz.
spawn-locked = Başlangıç noktası zaten başlka bir oyuncu slotuna kilitli.
admin-lobby-info = Sadece sunuzu lobi bilgisi gönderebilir.
invalid-lobby-info = Geçersiz lobi bilgisi gönderildi.
player-color-terrain = Renk araziden daha farklı olması için düzenlendi.
player-color-player = Renk başka bir oyuncununkinden daha farklı olması için düzenlendi.
invalid-player-color = Geçerli bir oyuncu rengi belirlenemedi. Rastgele bir renk seçildi.
invalid-error-code = Hata mesajı ayrıştırılamadı.
master-server-connected = Ana sunucu iletişimi kuruldu.
master-server-error = "Ana sunucu iletişimi başarısız oldu."
game-offline = Oyun çevrimiçi olarak gösterilemedi.
no-port-forward = Sunucu bağlantı noktası internetten erişilebilir değil.
blacklisted-title = Sunucu adı kara listeden bir kelime içeriyor.
requires-forum-account = Sunucu oyuncunun bir OpenRA forumu hesabının olmasını gerektiriyor.
no-permission = Bu sunucuya katılmaya izniniz yok.
slot-closed = Slotunuz sunucu tarafından kapatıldı.

## Server
game-started = Oyun başladı

## Server also LobbyUtils
bots-disabled = Botlar Devre Dışı

## ActorEditLogic
duplicate-actor-id = Kopya Aktör ID'si
enter-actor-id = Bir Aktör ID'si girin
owner = Sahip

## ActorSelectorLogic
type = Tür

## CommonSelectorLogic
search-results = Arama Sonuçları
multiple = Çok

## GameInfoLogic
objectives = Hedefler
briefing = Brifing
options = Seçenekler
debug = Hata Bulma
chat = Sohbet

## GameInfoObjectivesLogic also GameInfoStatsLogic
in-progress = Devam ediyor
accomplished = Tamamlandı
failed = Başarısız oldu

## GameTimerLogic
paused = Duraklatıldı
max-speed = Maks. Hız
speed = %{ $percentage } Hız
complete = %{ $percentage } tamamlandı

## LobbyLogic, InGameChatLogic
chat-availability =
    { $seconds ->
        [zero] Sohbet devre dışı
        *[other] Sohbet { $seconds } saniye içinde etkinleşecek...
    }

## IngamePowerBarLogic
## IngamePowerCounterLogic
power-usage = Güç Kullanımı

## IngameSiloBarLogic
## IngameCashCounterLogic
silo-usage = Depo Kullanımı: { $resources }/{ $capacity }

## ObserverShroudSelectorLogic
camera-option-all-players = Tüm Oyuncular
camera-option-disable-shroud = Karanlığı Devre Dışı Bırak
camera-option-other = Diğer

## ObserverStatsLogic
minimal = En Düşük
information-none = Bilgi: Hiçbiri
basic = Genel
economy = Ekonomi
production = Üretim
support-powers = Destek Güçler
combat = Savaş
army = Ordu
cps-and-upgrades = KG ve Geliştirmeler
earnings-graph = Gelir (grafik)
army-graph = Ordu (grafik)

## WorldTooltipLogic
unrevealed-terrain = Keşfedilmemiş Arazi

## ServerlistLogic, GameInfoStatsLogic, ObserverShroudSelectorLogic, SpawnSelectorTooltipLogic
team-no-team =
    { $team ->
        [zero] Takım Yok
       *[other] Takım { $team }
    }

## LobbyLogic, CommonSelectorLogic, InGameChatLogic
all = Tümü

## InputSettingsLogic, CommonSelectorLogic
none = Hiçbiri

## LobbyLogic, IngameChatLogic
team = Takım

## ServerListLogic, ReplayBrowserLogic also ObserverShroudSelectorLogic
players = Oyuncular
