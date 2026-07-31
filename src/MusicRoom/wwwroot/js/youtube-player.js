let player = null;
let dotNetRef = null;
let apiReadyPromise = null;

function ensureApiLoaded() {
    if (apiReadyPromise) return apiReadyPromise;
    apiReadyPromise = new Promise(resolve => {
        if (window.YT && window.YT.Player) {
            resolve();
            return;
        }
        window.onYouTubeIframeAPIReady = () => resolve();
        const tag = document.createElement('script');
        tag.src = "https://www.youtube.com/iframe_api";
        const first = document.getElementsByTagName('script')[0];
        first.parentNode.insertBefore(tag, first);
    });
    return apiReadyPromise;
}

function onPlayerStateChange(event) {
    if (event.data === YT.PlayerState.PLAYING && player && dotNetRef) {
        const duration = player.getDuration();
        if (duration > 0) {
            dotNetRef.invokeMethodAsync('OnDurationKnown', duration);
        }
    }
}

window.ytInterop = {
    init: async function (elementId, dotNetHelper) {
        dotNetRef = dotNetHelper;
        await ensureApiLoaded();
        if (player) return;
        player = new YT.Player(elementId, {
            height: '360',
            width: '640',
            playerVars: { autoplay: 1 },
            events: { onStateChange: onPlayerStateChange }
        });
    },

    loadVideo: async function (videoId, startSeconds) {
        await ensureApiLoaded();
        if (!player || !player.loadVideoById) {
            // Player element may not have finished constructing yet — retry shortly.
            setTimeout(() => window.ytInterop.loadVideo(videoId, startSeconds), 300);
            return;
        }
        player.loadVideoById({ videoId, startSeconds });
    },

    syncTime: function (expectedSeconds) {
        if (!player || !player.getCurrentTime) return;
        const drift = Math.abs(player.getCurrentTime() - expectedSeconds);
        // Only correct real desync — resyncing every tick would cause audible stutter.
        if (drift > 2.5) {
            player.seekTo(expectedSeconds, true);
        }
    }
};
