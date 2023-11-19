export const getApp = () => new class {

    /** @type {MediaStream} */ #currStream;

    disposeStream() {
        if (this.#currStream) {
            for (const t of this.#currStream.getTracks()) {
                t.stop();
            }
        }
    }
    
    async requestStreamAsync(audio, video) {
        this.disposeStream();

        const stream = this.#currStream =
            await navigator.mediaDevices.getUserMedia({
                audio,
                video,
            });

        if (!stream) {
            throw new Error("NO_STREAM");
        }

        const vid = this.#preview;
        if (vid) {
            vid.srcObject = stream;
        }
    }

    async requestGeoAsync() {
        return await new Promise((res, rej) => {
            navigator.geolocation.getCurrentPosition(
                ({ coords }) => res(`${coords.latitude},${coords.longitude}`),
                rej);
        });
    }

    get #preview() {
        return document.querySelector("video");
    }

}();