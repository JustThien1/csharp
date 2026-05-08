// ocr.js — Tesseract.js helper cho Blazor WASM
// Được gọi qua IJSRuntime từ Subscription.razor

window.ocrHelper = {
    /**
     * Nhận dạng văn bản từ ảnh bằng Tesseract.js.
     * @param {string} dataUrl - Data URL của ảnh (base64)
     * @param {string} lang    - Ngôn ngữ, mặc định 'eng+vie'
     * @returns {Promise<string>} Văn bản nhận dạng được
     */
    recognize: async function (dataUrl, lang) {
        const { data: { text } } = await Tesseract.recognize(
            dataUrl,
            lang || 'eng+vie',
            { logger: () => {} }
        );
        return text;
    }
};
