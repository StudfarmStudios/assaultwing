window.assaultWingBrowser = {
    loadSettings(key) {
        return window.localStorage.getItem(key);
    },

    saveSettings(key, xml) {
        window.localStorage.setItem(key, xml);
    },

    openUrl(url) {
        const link = document.createElement("a");
        link.href = url;
        link.target = "_blank";
        link.rel = "noopener noreferrer";
        link.click();
    },

    pulseKniKey(keyCode, character = 0) {
        const windowUid = globalThis.nkJSObject?.GetUid(window) ?? -1;
        if (windowUid < 0)
            return;

        DotNet.invokeMethod("nkast.Wasm.Dom", "JsWindowOnKeyDown",
            windowUid, character, keyCode, 0);
        window.setTimeout(() => DotNet.invokeMethod(
            "nkast.Wasm.Dom", "JsWindowOnKeyUp",
            windowUid, character, keyCode, 0), 80);
    },

    updateAudioStatus() {
        const objects = globalThis.nkJSObject?.objectMap ?? [];
        const contexts = objects.filter(object => object instanceof AudioContext);
        const music = objects.filter(object => object instanceof HTMLAudioElement);
        const canvas = document.getElementById("theCanvas");
        if (canvas) {
            canvas.dataset.audioContexts = String(contexts.length);
            canvas.dataset.audioContextsRunning = String(
                contexts.filter(context => context.state === "running").length);
            canvas.dataset.musicElements = String(music.length);
            canvas.dataset.musicPlaying = String(
                music.filter(element => !element.paused && !element.ended).length);
        }
    },

    async unlockAudio() {
        const objects = globalThis.nkJSObject?.objectMap ?? [];
        const contexts = objects.filter(object => object instanceof AudioContext);
        const music = objects.filter(object => object instanceof HTMLAudioElement && object.src);

        await Promise.allSettled(contexts
            .filter(context => context.state === "suspended")
            .map(context => context.resume()));
        await Promise.allSettled(music
            .filter(element => element.paused && !element.ended)
            .map(element => element.play()));

        this.updateAudioStatus();
        console.info(`[AW2 Audio] unlocked: ${contexts.filter(context => context.state === "running").length}/${contexts.length} Web Audio contexts running, ${music.filter(element => !element.paused && !element.ended).length}/${music.length} music tracks playing`);
    },

    start(instance) {
        // KNI 4.2's JavaScript interop layer predates .NET 9 and reads the
        // Emscripten heap through the global Module object. .NET 9 keeps that
        // object on its runtime API, so expose the compatibility alias before
        // the first KNI frame runs.
        const runtime = typeof getDotnetRuntime === "function"
            ? getDotnetRuntime(0)
            : null;
        if (!runtime?.Module)
            throw new Error("The .NET WebAssembly runtime is unavailable.");
        globalThis.Module = runtime.Module;
        globalThis.BINDING = {
            conv_string: runtime.INTERNAL.monoStringToStringUnsafe,
        };
        if (typeof Blazor !== "undefined" && Blazor.platform) {
            Blazor.platform.getArrayLength ??= arrayAddress =>
                runtime.getHeapI32(arrayAddress + 12);
            Blazor.platform.readObject ??= address =>
                runtime.getHeapU32(address);
        }

        const canvas = document.getElementById("theCanvas");
        const holder = document.getElementById("canvas-holder");

        const resize = () => {
            // KNI reports mouse coordinates in CSS pixels, so keep the backing
            // buffer in that same coordinate space.
            canvas.width = Math.max(1, Math.floor(holder.clientWidth));
            canvas.height = Math.max(1, Math.floor(holder.clientHeight));
        };

        resize();
        window.addEventListener("resize", resize);
        canvas.addEventListener("contextmenu", event => event.preventDefault());
        canvas.addEventListener("pointerdown", () => canvas.focus({ preventScroll: true }));
        // The original game UI is keyboard/gamepad driven. Double-clicking the
        // focused browser canvas provides a natural pointer equivalent for its
        // browser-only Space/activate control.
        canvas.addEventListener("dblclick", event => {
            event.preventDefault();
            assaultWingBrowser.pulseKniKey(32, 32);
        });
        canvas.focus({ preventScroll: true });

        const tick = () => {
            instance.invokeMethod("TickDotNet");
            window.requestAnimationFrame(tick);
        };
        window.requestAnimationFrame(tick);
    },
};

// Add observable browser-side audio diagnostics without changing KNI itself.
// This also makes autoplay reliable: the first key/pointer gesture resumes the
// Web Audio context and retries any menu music blocked during initial loading.
if (globalThis.nkAudioContext) {
    for (const methodName of ["Create", "Create1"]) {
        const original = nkAudioContext[methodName];
        nkAudioContext[methodName] = function (...args) {
            const uid = original.apply(this, args);
            const context = nkJSObject.GetObject(uid);
            context.addEventListener("statechange", () => assaultWingBrowser.updateAudioStatus());
            assaultWingBrowser.updateAudioStatus();
            return uid;
        };
    }
}

if (globalThis.nkAudio) {
    const originalCreateAudio = nkAudio.Create;
    nkAudio.Create = function (...args) {
        const uid = originalCreateAudio.apply(this, args);
        const element = nkJSObject.GetObject(uid);
        element.addEventListener("playing", () => {
            assaultWingBrowser.updateAudioStatus();
            console.info(`[AW2 Audio] music is playing: ${element.currentSrc || element.src}`);
        });
        element.addEventListener("pause", () => assaultWingBrowser.updateAudioStatus());
        return uid;
    };
}

if (globalThis.nkMedia) {
    // KNI 4.2 declares this interop return value as an integer. Modern
    // browsers expose sub-millisecond fractions and .NET 9 validates the JS
    // number strictly, so normalize at the compatibility boundary.
    nkMedia.GetCurrentTime = function (uid) {
        return Math.round(nkJSObject.GetObject(uid).currentTime * 1000);
    };

    // KNI intentionally does not await HTMLMediaElement.play(). Catch the
    // expected autoplay rejection; unlockAudio retries on the user's gesture.
    nkMedia.Play = function (uid) {
        const mediaElement = nkJSObject.GetObject(uid);
        const playPromise = mediaElement.play();
        playPromise?.catch(error => {
            if (error?.name !== "NotAllowedError")
                console.error("Assault Wing music playback failed", error);
            assaultWingBrowser.updateAudioStatus();
        });
    };
}

window.addEventListener("pointerdown", () => assaultWingBrowser.unlockAudio(), { capture: true });
window.addEventListener("keydown", () => assaultWingBrowser.unlockAudio(), { capture: true });

window.addEventListener("keydown", event => {
    if ([32, 37, 38, 39, 40].includes(event.keyCode))
        event.preventDefault();
});

window.addEventListener("wheel", event => event.preventDefault(), { passive: false });
