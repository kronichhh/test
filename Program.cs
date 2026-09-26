using System.Net;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Разрешаем сайту показывать файлы из wwwroot (в том числе картинки).
app.UseStaticFiles();

app.MapGet("/", () =>
{
    var imageFolder = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "images");
    Directory.CreateDirectory(imageFolder);

    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    var images = Directory.GetFiles(imageFolder)
        .Where(file => allowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
        .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
        .Select(file => new
        {
            FileName = Path.GetFileName(file),
            Title = Path.GetFileNameWithoutExtension(file)
        })
        .ToList();

    var gallery = images.Count == 0
        ? """
          <div class="empty-gallery">
              <div>YOUR WORKS WILL APPEAR HERE</div>
              <small>Закинь JPG / PNG / WEBP в папку wwwroot/images</small>
          </div>
          """
        : string.Join("\n", images.Select((image, index) =>
        {
            var url = "/images/" + Uri.EscapeDataString(image.FileName).Replace("%2F", "/");
            var title = WebUtility.HtmlEncode(image.Title);
            var escapedUrl = WebUtility.HtmlEncode(url);

            return $"""
            <button class="art-card" onclick="openWork('{escapedUrl}','{title}')">
                <img src="{escapedUrl}" alt="{title}" loading="lazy">
                <span class="work-number">{(index + 1).ToString("00")}</span>
                <span class="work-title">{title}</span>
            </button>
            """;
        }));

    var html = $$"""
<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>ALISA — Artist Portfolio</title>
    <style>
        * { box-sizing: border-box; }
        html { scroll-behavior: smooth; }
        body {
            margin: 0;
            background: #080808;
            color: #dedbd5;
            font-family: Arial, sans-serif;
            overflow-x: hidden;
        }
        a { color: inherit; text-decoration: none; }
        button { font: inherit; }

        .topbar {
            height: 82px;
            position: fixed;
            inset: 0 0 auto 0;
            z-index: 20;
            display: grid;
            grid-template-columns: 1fr auto 1fr;
            align-items: center;
            padding: 0 4.7vw;
            border-bottom: 1px solid #292929;
            background: rgba(8,8,8,.96);
        }
        .logo {
            font-size: 25px;
            font-style: italic;
            letter-spacing: .08em;
        }
        nav { display: flex; gap: 48px; }
        nav a, .socials a { font: 11px monospace; color: #8c8a86; }
        nav a:hover, .socials a:hover { color: #fff; }
        .socials { justify-self: end; display: flex; gap: 20px; }

        .hero {
            min-height: 540px;
            margin-top: 82px;
            display: grid;
            grid-template-columns: 42% 58%;
            border-bottom: 1px solid #292929;
        }
        .hero-copy {
            display: flex;
            flex-direction: column;
            justify-content: center;
            padding: 55px 4.7vw;
        }
        .intro {
            margin-bottom: 45px;
            color: #999;
            font: 11px/1.6 monospace;
        }
        h1 {
            margin: 0 0 45px;
            font-size: clamp(42px, 5vw, 74px);
            font-weight: 400;
            line-height: .94;
            letter-spacing: -.06em;
        }
        .view {
            width: max-content;
            padding-bottom: 7px;
            border-bottom: 1px solid #555;
            font: 11px monospace;
        }
        .view span { margin-left: 15px; font-size: 17px; }

        .hero-art {
            min-height: 458px;
            position: relative;
            overflow: hidden;
            background: linear-gradient(125deg,#111,#302d29 55%,#5a554e);
        }
        .hero-art::before {
            content: "";
            position: absolute;
            inset: 0;
            background: repeating-linear-gradient(115deg,transparent 0 18px,rgba(255,255,255,.05) 19px 21px,transparent 22px 50px);
        }
        .hero-placeholder {
            position: absolute;
            inset: 0;
            display: grid;
            place-items: center;
            color: #aaa;
            font: 10px monospace;
            letter-spacing: .08em;
        }
        .hero-image {
            position: absolute;
            inset: 0;
            width: 100%;
            height: 100%;
            object-fit: cover;
            display: block;
        }

        .works { padding: 55px 4.7vw 80px; border-bottom: 1px solid #292929; }
        .works-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 30px; }
        .works h2 { margin: 0; font-size: 22px; font-weight: 400; letter-spacing: .18em; }
        .works-head a { font: 10px monospace; color: #999; }
        .gallery {
            display: grid;
            grid-template-columns: 1.08fr 1fr 1fr .9fr;
            grid-auto-rows: 220px;
            gap: 18px;
        }
        .art-card {
            position: relative;
            min-width: 0;
            padding: 0;
            border: 0;
            overflow: hidden;
            cursor: pointer;
            background: #202020;
            color: #ddd;
        }
        .art-card:first-child { grid-row: span 2; }
        .art-card img {
            width: 100%;
            height: 100%;
            display: block;
            object-fit: cover;
            transition: transform .45s ease, filter .45s ease;
        }
        .art-card:hover img { transform: scale(1.035); filter: brightness(1.1); }
        .art-card::after {
            content: "";
            position: absolute;
            inset: 0;
            background: linear-gradient(transparent 55%,rgba(0,0,0,.72));
            pointer-events: none;
        }
        .work-number, .work-title {
            position: absolute;
            z-index: 2;
            bottom: 13px;
            font: 9px monospace;
        }
        .work-number { left: 13px; color: #aaa; }
        .work-title { right: 13px; max-width: 70%; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
        .empty-gallery {
            min-height: 260px;
            display: grid;
            place-items: center;
            text-align: center;
            border: 1px solid #292929;
            color: #777;
            font: 12px monospace;
        }
        .empty-gallery small { display: block; margin-top: 10px; color: #555; }

        .about {
            padding: 100px 4.7vw;
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 10vw;
            border-bottom: 1px solid #292929;
        }
        .about-title { font-size: clamp(45px,6vw,80px); line-height: .88; letter-spacing: -.07em; }
        .about-text { max-width: 480px; color: #999; font-size: 16px; line-height: 1.65; }
        .about-text p:first-child { color: #dedbd5; font-size: 21px; }

        footer {
            min-height: 115px;
            padding: 32px 4.7vw 26px;
            display: grid;
            grid-template-columns: 1.2fr 3fr 1fr;
            align-items: center;
        }
        .footer-left { font: 13px/1.35 monospace; transform: rotate(-4deg); }
        .footer-links { display: flex; justify-content: center; gap: 38px; flex-wrap: wrap; }
        .footer-links a { font: 9px monospace; color: #888; }
        .footer-links a:hover { color: #ddd; }
        .thanks { justify-self: end; text-align: center; color: #777; font: 8px/1.6 monospace; }

        .modal {
            position: fixed;
            inset: 0;
            z-index: 100;
            display: none;
            place-items: center;
            padding: 40px;
            background: rgba(0,0,0,.96);
        }
        .modal.open { display: grid; }
        .modal img { max-width: 92vw; max-height: 88vh; object-fit: contain; }
        .modal-title { position: absolute; left: 30px; bottom: 25px; color: #aaa; font: 10px monospace; }
        .close {
            position: absolute;
            top: 15px;
            right: 25px;
            z-index: 2;
            border: 0;
            background: transparent;
            color: white;
            font-size: 38px;
            cursor: pointer;
        }

        @media (max-width: 800px) {
            .topbar { grid-template-columns: 1fr auto; padding: 0 20px; }
            nav { display: none; }
            .hero { grid-template-columns: 1fr; }
            .hero-copy { min-height: 420px; }
            .hero-art { min-height: 400px; }
            .gallery { grid-template-columns: 1fr 1fr; grid-auto-rows: 220px; }
            .art-card:first-child { grid-row: span 1; }
            .about { grid-template-columns: 1fr; gap: 45px; }
            footer { grid-template-columns: 1fr; gap: 28px; }
            .thanks { justify-self: start; }
            .footer-links { justify-content: flex-start; }
        }
        @media (max-width: 520px) {
            .works, .about, footer { padding-left: 20px; padding-right: 20px; }
            .gallery { grid-template-columns: 1fr; }
            .art-card { height: 280px; }
        }
    </style>
</head>
<body>
<header class="topbar">
    <a class="logo" href="#home">ALISA</a>
    <nav>
        <a href="#home">HOME</a>
        <a href="#works">WORKS</a>
        <a href="#about">ABOUT</a>
    </nav>
    <div class="socials">
        <!-- ЗАМЕНИ ТОЛЬКО ЭТИ ССЫЛКИ -->
        <a href="https://instagram.com/YOUR_USERNAME" target="_blank">◎</a>
        <a href="https://t.me/YOUR_USERNAME" target="_blank">➤</a>
        <a href="https://vk.com/YOUR_USERNAME" target="_blank">ᴠᴋ</a>
        <a href="https://x.com/YOUR_USERNAME" target="_blank">𝕏</a>
    </div>
</header>

<section class="hero" id="home">
    <div class="hero-copy">
        <div class="intro">HI, I'M ALISA<br>I'M AN ARTIST</div>
        <h1>ART IS HOW<br>I STAY SANE.</h1>
        <a class="view" href="#works">VIEW MY WORKS <span>→</span></a>
    </div>
    <div class="hero-art">
        <div class="hero-placeholder">YOUR HERO IMAGE</div>
    </div>
</section>

<section class="works" id="works">
    <div class="works-head">
        <h2>SELECTED WORKS</h2>
        <a href="#works">ALL WORKS →</a>
    </div>
    <div class="gallery">
        {{gallery}}
    </div>
</section>

<section class="about" id="about">
    <div class="about-title">ABOUT<br>THE ARTIST</div>
    <div class="about-text">
        <p>Привет, я Алиса — художница.</p>
        <p>Здесь можно написать информацию о себе, своих техниках, проектах и том, что ты создаёшь.</p>
    </div>
</section>

<footer>
    <div class="footer-left">LET'S<br>BE IN TOUCH ✳</div>
    <div class="footer-links">
        <!-- ЗАМЕНИ ТОЛЬКО ЭТИ ССЫЛКИ -->
        <a href="https://instagram.com/YOUR_USERNAME" target="_blank">INSTAGRAM →</a>
        <a href="https://t.me/YOUR_USERNAME" target="_blank">TELEGRAM →</a>
        <a href="https://vk.com/YOUR_USERNAME" target="_blank">VK →</a>
        <a href="https://x.com/YOUR_USERNAME" target="_blank">X →</a>
    </div>
    <div class="thanks">THANK YOU<br>FOR BEING HERE<br>♡</div>
</footer>

<div class="modal" id="modal">
    <button class="close" onclick="closeWork()">×</button>
    <img id="modalImage" src="" alt="">
    <div class="modal-title" id="modalTitle"></div>
</div>

<script>
function openWork(url, title) {
    document.getElementById('modalImage').src = url;
    document.getElementById('modalImage').alt = title;
    document.getElementById('modalTitle').textContent = title;
    document.getElementById('modal').classList.add('open');
    document.body.style.overflow = 'hidden';
}
function closeWork() {
    document.getElementById('modal').classList.remove('open');
    document.getElementById('modalImage').src = '';
    document.body.style.overflow = '';
}
document.getElementById('modal').addEventListener('click', function(e) {
    if (e.target === this) closeWork();
});
document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') closeWork();
});
</script>
</body>
</html>
""";

    return Results.Content(html, "text/html; charset=utf-8");
});

app.Run();
