(() => {
    "use strict";

    const POLL_INTERVAL_MS = 250;

    let currentPlayer = null;
    let currentItemId = null;
    let segments = [];
    let lastSeekTarget = null;

    async function loadSegments(itemId) {
        try {
            const response = await ApiClient.fetch({
                url: `MediaSegments/${itemId}`,
            });

            if (!Array.isArray(response)) {
                segments = [];
                return;
            }

            segments = response.filter((x) => x.type === "Recap").sort((a, b) => a.startTicks - b.startTicks);
        } catch (e) {
            console.error("Skip Time: failed to load media segments.", e);
            segments = [];
        }
    }

    async function onPlaybackStart(player) {
        currentPlayer = player;

        const item = player.getCurrentItem?.();

        if (!item) {
            return;
        }

        currentItemId = item.Id;

        await loadSegments(currentItemId);

        lastSeekTarget = null;
    }

    function onPlaybackStopped() {
        currentPlayer = null;
        currentItemId = null;
        segments = [];
        lastSeekTarget = null;
    }

    function checkPlayback() {
        if (!currentPlayer) {
            return;
        }

        if (!segments.length) {
            return;
        }

        const positionTicks = currentPlayer.currentTime() * 10000000;

        for (const segment of segments) {
            if (positionTicks >= segment.startTicks && positionTicks < segment.endTicks) {
                if (lastSeekTarget === segment.endTicks) {
                    return;
                }

                lastSeekTarget = segment.endTicks;

                console.log("Skip Time: skipping recap.", segment);

                currentPlayer.currentTime(segment.endTicks / 10000000);

                return;
            }
        }

        lastSeekTarget = null;
    }

    function hookPlaybackEvents() {
        events.on("playbackstart", async (e) => {
            const player = window.MediaPlayer ?? window.player ?? e.player;

            if (!player) {
                return;
            }

            await onPlaybackStart(player);
        });

        events.on("playbackstop", onPlaybackStopped);
    }

    hookPlaybackEvents();

    setInterval(checkPlayback, POLL_INTERVAL_MS);
})();
