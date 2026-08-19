// OpenLearning video lesson player: speed, resolution, resume, cast, seek lock,
// and danmu (bullet comments) via the course-chat hub.
(function () {
    const video = document.getElementById('lesson-video');
    if (!video) return;

    const courseId = parseInt(video.dataset.courseId, 10);
    const lessonId = parseInt(video.dataset.lessonId, 10);
    const protectedMode = video.dataset.protected === 'true';
    const sources = JSON.parse(video.dataset.sources || '[]');
    const existingDanmu = JSON.parse(video.dataset.danmu || '[]');
    const saveIntervalMs = 5000;

    // ---- source / resolution ----
    let currentSource = sources.length ? sources[0].src : null;
    if (currentSource) {
        video.src = currentSource;
    }

    const resolutionSelect = document.getElementById('resolution-select');
    if (resolutionSelect && sources.length > 1) {
        sources.forEach(function (source, index) {
            const option = document.createElement('option');
            option.value = String(index);
            option.textContent = source.label;
            resolutionSelect.appendChild(option);
        });
        resolutionSelect.addEventListener('change', function () {
            const source = sources[parseInt(this.value, 10)];
            if (!source) return;
            const position = video.currentTime;
            video.src = source.src;
            video.currentTime = position;
            video.play();
        });
    } else if (resolutionSelect) {
        const option = document.createElement('option');
        option.value = '0';
        option.textContent = 'Auto';
        resolutionSelect.appendChild(option);
    }

    // ---- speed ----
    const speedSelect = document.getElementById('speed-select');
    if (speedSelect) {
        speedSelect.addEventListener('change', function () {
            video.playbackRate = parseFloat(this.value);
        });
    }

    // ---- cast (best-effort, hidden unless the browser supports Media Session) ----
    const castButton = document.getElementById('cast-button');
    if (castButton && 'mediaSession' in navigator) {
        castButton.style.display = '';
        castButton.addEventListener('click', function () {
            navigator.mediaSession.metadata = new MediaMetadata({ title: document.title });
            if (video.requestFullscreen) {
                video.requestFullscreen().catch(function () { /* user gesture may be required */ });
            }
        });
    }

    // ---- resume + position saving ----
    let lastPosition = 0;
    let restoring = false;

    async function loadPosition() {
        try {
            const response = await fetch('/progress/position?courseId=' + courseId + '&lessonId=' + lessonId);
            const data = await response.json();
            if (data && data.seconds > 0) {
                restoring = true;
                lastPosition = data.seconds;
                video.currentTime = data.seconds;
            }
        } catch (e) { /* resume is best-effort */ }
    }

    async function savePosition() {
        if (video.readyState === 0) return;
        const seconds = Math.floor(video.currentTime);
        try {
            await fetch('/progress/position', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ courseId: courseId, lessonId: lessonId, seconds: seconds })
            });
        } catch (e) { /* best-effort */ }
    }

    video.addEventListener('timeupdate', function () {
        lastPosition = video.currentTime;
    });
    video.addEventListener('seeked', function () {
        restoring = false;
    });
    video.addEventListener('pause', savePosition);
    window.addEventListener('pagehide', savePosition);

    // ---- seek lock (protected mode: lock back to the position before the seek) ----
    video.addEventListener('seeking', function () {
        if (protectedMode && !restoring && video.currentTime !== lastPosition) {
            video.currentTime = lastPosition;
        }
    });

    setInterval(savePosition, saveIntervalMs);
    loadPosition();

    // ---- danmu ----
    const danmuLayer = document.getElementById('danmu-layer');
    const danmuInput = document.getElementById('danmu-input');
    const danmuSend = document.getElementById('danmu-send');
    if (!danmuLayer || !danmuInput || !danmuSend || typeof signalR === 'undefined') return;

    const colors = ['#e74c3c', '#f39c12', '#27ae60', '#2980b9', '#8e44ad', '#16a085'];

    function spawnBullet(text) {
        const el = document.createElement('div');
        el.className = 'danmu-bullet';
        el.textContent = text;
        el.style.color = colors[Math.floor(Math.random() * colors.length)];
        el.style.top = (8 + Math.random() * 30) + '%';
        el.style.animationDuration = (6 + Math.random() * 4) + 's';
        danmuLayer.appendChild(el);
        el.addEventListener('animationend', function () { el.remove(); });
    }

    // Replay existing danmu as a burst when the page loads.
    existingDanmu.forEach(function (item, index) {
        setTimeout(function () { spawnBullet(item.Body); }, index * 700);
    });

    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/course-chat')
        .build();

    connection.on('ReceiveDanmu', function (userName, body) {
        spawnBullet(body);
    });

    connection.start().then(function () {
        return connection.invoke('JoinCourse', courseId);
    }).catch(function (err) {
        console.error('danmu connection failed', err);
    });

    danmuSend.addEventListener('click', sendDanmu);
    danmuInput.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') sendDanmu();
    });

    function sendDanmu() {
        const body = danmuInput.value.trim();
        if (!body) return;
        connection.invoke('SendDanmu', courseId, lessonId, body).catch(function (err) {
            console.error(err);
        });
        danmuInput.value = '';
    }
})();
