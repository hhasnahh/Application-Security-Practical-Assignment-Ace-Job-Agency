// ===============================
// Password show/hide (eye button)
// ===============================
window.togglePw = function (id, btn) {
    const input = document.getElementById(id);
    if (!input) return;

    if (input.type === "password") {
        input.type = "text";
        btn.textContent = "👁";
    } else {
        input.type = "password";
        btn.textContent = "🙈";
    }
};

// =====================================================
// DOM Ready: password rules checker + generator + copy
// =====================================================
document.addEventListener("DOMContentLoaded", function () {

    // ---------------------------
    // A) Live password rules UI (Register only)
    // Uses IDs: pw, rLen rUp rLow rNum rSym rSpace
    // ---------------------------
    const pw = document.getElementById("pw");

    const setRule = (id, ok) => {
        const el = document.getElementById(id);
        if (!el) return;

        el.textContent =
            (ok ? "✅ " : "❌ ") +
            el.textContent.replace(/^✅ |^❌ /, "");
    };

    if (pw) {
        pw.addEventListener("input", function () {
            let v = pw.value;

            // ✅ Block spaces instantly
            if (/\s/.test(v)) {
                pw.value = v.replace(/\s/g, "");
                v = pw.value;
            }

            setRule("rLen", v.length >= 12);
            setRule("rUp", /[A-Z]/.test(v));
            setRule("rLow", /[a-z]/.test(v));
            setRule("rNum", /\d/.test(v));
            setRule("rSym", /[^A-Za-z0-9]/.test(v));
            setRule("rSpace", !/\s/.test(v));
        });
    }

    // ---------------------------
    // B) Strong random generator + copy (reusable)
    // Buttons:
    //  - data-pwgen data-target="inputId" data-confirm="confirmInputId"
    //  - data-pwcopy data-target="inputId"
    // ---------------------------

    function randChar(chars) {
        const arr = new Uint32Array(1);
        crypto.getRandomValues(arr);
        return chars[arr[0] % chars.length];
    }

    function shuffle(str) {
        const a = str.split("");
        for (let i = a.length - 1; i > 0; i--) {
            const r = new Uint32Array(1);
            crypto.getRandomValues(r);
            const j = r[0] % (i + 1);
            [a[i], a[j]] = [a[j], a[i]];
        }
        return a.join("");
    }

    function generatePassword(len = 16) {
        const upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const lower = "abcdefghijklmnopqrstuvwxyz";
        const digits = "0123456789";
        const symbols = "!@#$%^&*()-_=+[]{};:,.?"; // no spaces

        // Ensure at least one of each
        let out = "";
        out += randChar(upper);
        out += randChar(lower);
        out += randChar(digits);
        out += randChar(symbols);

        const all = upper + lower + digits + symbols;
        while (out.length < len) out += randChar(all);

        return shuffle(out);
    }

    // Generate
    document.querySelectorAll("[data-pwgen]").forEach(btn => {
        btn.addEventListener("click", function () {
            const targetId = btn.getAttribute("data-target");
            if (!targetId) return;

            const input = document.getElementById(targetId);
            if (!input) return;

            input.value = generatePassword(16);

            // Autofill confirm if specified
            const confirmId = btn.getAttribute("data-confirm");
            if (confirmId) {
                const confirmInput = document.getElementById(confirmId);
                if (confirmInput) confirmInput.value = input.value;
            }

            // Trigger validators + checklist updates
            input.dispatchEvent(new Event("input", { bubbles: true }));
            input.dispatchEvent(new Event("change", { bubbles: true }));

            if (confirmId) {
                const confirmInput = document.getElementById(confirmId);
                if (confirmInput) {
                    confirmInput.dispatchEvent(new Event("input", { bubbles: true }));
                    confirmInput.dispatchEvent(new Event("change", { bubbles: true }));
                }
            }
        });
    });

    // Copy
    document.querySelectorAll("[data-pwcopy]").forEach(btn => {
        btn.addEventListener("click", async function () {
            const targetId = btn.getAttribute("data-target");
            if (!targetId) return;

            const input = document.getElementById(targetId);
            if (!input) return;

            try {
                await navigator.clipboard.writeText(input.value);
                const old = btn.textContent;
                btn.textContent = "Copied!";
                setTimeout(() => (btn.textContent = old), 900);
            } catch {
                // Fallback copy
                input.focus();
                input.select();
                document.execCommand("copy");
            }
        });
    });

});
