/**
 * TürkiyAI Embeddable Chat Widget
 *
 * Usage (Enterprise plan):
 *   <script src="https://turkai.io/widget.js"
 *           data-api-key="tk_your_api_key"
 *           data-language="en"
 *           data-theme="light"
 *           data-position="bottom-right">
 *   </script>
 *
 * The widget injects a floating chat button and slide-up panel into the host page.
 * It communicates with the TürkiyAI REST API using the provided API key.
 */
(function () {
    'use strict';

    var script = document.currentScript ||
        (function () {
            var scripts = document.getElementsByTagName('script');
            return scripts[scripts.length - 1];
        })();

    var API_KEY   = script.getAttribute('data-api-key')  || '';
    var LANGUAGE  = script.getAttribute('data-language') || 'en';
    var THEME     = script.getAttribute('data-theme')    || 'light';
    var POSITION  = script.getAttribute('data-position') || 'bottom-right';
    var API_BASE  = script.getAttribute('data-api-base') || 'https://turkai.io';

    // ── Styles ────────────────────────────────────────────────────────────────
    var css = [
        '.turkai-widget-btn{position:fixed;width:56px;height:56px;border-radius:50%;',
        'background:linear-gradient(135deg,#c0392b,#e74c3c);color:#fff;',
        'border:none;cursor:pointer;box-shadow:0 4px 14px rgba(0,0,0,.25);',
        'font-size:26px;display:flex;align-items:center;justify-content:center;',
        'z-index:2147483646;transition:transform .2s;}',
        '.turkai-widget-btn:hover{transform:scale(1.08);}',
        POSITION === 'bottom-left'
            ? '.turkai-widget-btn{bottom:20px;left:20px;}'
            : '.turkai-widget-btn{bottom:20px;right:20px;}',
        '.turkai-widget-panel{position:fixed;',
        POSITION === 'bottom-left' ? 'bottom:90px;left:20px;' : 'bottom:90px;right:20px;',
        'width:340px;max-width:calc(100vw - 40px);',
        'border-radius:12px;overflow:hidden;',
        'box-shadow:0 8px 32px rgba(0,0,0,.22);',
        'display:none;flex-direction:column;z-index:2147483645;',
        'font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",sans-serif;',
        'font-size:14px;}',
        '.turkai-widget-panel.open{display:flex;}',
        THEME === 'dark'
            ? '.turkai-widget-panel{background:#1e1e2e;color:#cdd6f4;}'
            : '.turkai-widget-panel{background:#fff;color:#333;}',
        '.turkai-widget-header{background:linear-gradient(135deg,#c0392b,#e74c3c);',
        'color:#fff;padding:12px 16px;display:flex;align-items:center;gap:10px;}',
        '.turkai-widget-header span{font-weight:600;flex:1;}',
        '.turkai-widget-close{background:none;border:none;color:#fff;',
        'cursor:pointer;font-size:18px;line-height:1;padding:0;}',
        '.turkai-widget-messages{flex:1;overflow-y:auto;padding:12px;',
        'max-height:300px;display:flex;flex-direction:column;gap:8px;}',
        '.turkai-widget-msg{max-width:80%;padding:8px 12px;border-radius:16px;',
        'line-height:1.4;word-break:break-word;}',
        THEME === 'dark'
            ? '.turkai-widget-msg.user{align-self:flex-end;background:#c0392b;color:#fff;}'
              + '.turkai-widget-msg.ai{align-self:flex-start;background:#313244;color:#cdd6f4;}'
            : '.turkai-widget-msg.user{align-self:flex-end;background:#c0392b;color:#fff;}'
              + '.turkai-widget-msg.ai{align-self:flex-start;background:#f4f4f4;color:#333;}',
        '.turkai-widget-typing{align-self:flex-start;padding:8px 14px;',
        THEME === 'dark' ? 'background:#313244;' : 'background:#f4f4f4;',
        'border-radius:16px;display:none;}',
        '.turkai-widget-typing span{display:inline-block;width:6px;height:6px;',
        'border-radius:50%;background:#999;animation:turkai-blink 1.2s infinite;margin:0 2px;}',
        '.turkai-widget-typing span:nth-child(2){animation-delay:.2s;}',
        '.turkai-widget-typing span:nth-child(3){animation-delay:.4s;}',
        '@keyframes turkai-blink{0%,80%,100%{opacity:0;}40%{opacity:1;}}',
        '.turkai-widget-input-row{display:flex;gap:8px;padding:10px 12px;',
        THEME === 'dark' ? 'border-top:1px solid #313244;' : 'border-top:1px solid #eee;',
        '}',
        '.turkai-widget-input{flex:1;padding:8px 12px;border-radius:20px;',
        'border:1px solid #ddd;outline:none;font-size:13px;',
        THEME === 'dark' ? 'background:#313244;color:#cdd6f4;border-color:#45475a;' : '',
        '}',
        '.turkai-widget-send{background:#c0392b;color:#fff;border:none;',
        'border-radius:20px;padding:8px 16px;cursor:pointer;font-size:13px;',
        'transition:background .15s;}',
        '.turkai-widget-send:hover{background:#a93226;}',
        '.turkai-widget-send:disabled{opacity:.5;cursor:default;}',
        '.turkai-widget-footer{text-align:center;padding:6px;font-size:11px;',
        THEME === 'dark' ? 'color:#6c7086;' : 'color:#aaa;',
        '}',
    ].join('');

    var style = document.createElement('style');
    style.textContent = css;
    document.head.appendChild(style);

    // ── DOM ───────────────────────────────────────────────────────────────────
    var btn = document.createElement('button');
    btn.className = 'turkai-widget-btn';
    btn.setAttribute('aria-label', 'Open TürkiyAI chat');
    btn.textContent = '🇹🇷';

    var panel = document.createElement('div');
    panel.className = 'turkai-widget-panel';
    panel.setAttribute('role', 'dialog');
    panel.setAttribute('aria-label', 'TürkiyAI chat');

    var greeting = LANGUAGE === 'tr'
        ? 'Merhaba! Türkiye seyahati hakkında size nasıl yardımcı olabilirim?'
        : 'Hello! How can I help you plan your Türkiye adventure?';

    panel.innerHTML = [
        '<div class="turkai-widget-header">',
        '  <span>🇹🇷 TürkiyAI</span>',
        '  <button class="turkai-widget-close" aria-label="Close chat">✕</button>',
        '</div>',
        '<div class="turkai-widget-messages" id="turkai-msgs">',
        '  <div class="turkai-widget-msg ai">' + _escapeHtml(greeting) + '</div>',
        '  <div class="turkai-widget-typing" id="turkai-typing">',
        '    <span></span><span></span><span></span>',
        '  </div>',
        '</div>',
        '<div class="turkai-widget-input-row">',
        '  <input class="turkai-widget-input" id="turkai-input" type="text"',
        '         placeholder="' + (LANGUAGE === 'tr' ? 'Bir şey sorun...' : 'Ask about Türkiye...') + '" />',
        '  <button class="turkai-widget-send" id="turkai-send">',
        LANGUAGE === 'tr' ? 'Gönder' : 'Send',
        '  </button>',
        '</div>',
        '<div class="turkai-widget-footer">Powered by <a href="https://turkai.io" target="_blank" rel="noopener">TürkiyAI</a></div>',
    ].join('');

    document.body.appendChild(btn);
    document.body.appendChild(panel);

    // ── State ─────────────────────────────────────────────────────────────────
    var history = [];
    var sessionId = null;
    var isLoading = false;

    // ── Helpers ───────────────────────────────────────────────────────────────
    function _escapeHtml(str) {
        return str
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function _getMsgs()  { return document.getElementById('turkai-msgs'); }
    function _getInput() { return document.getElementById('turkai-input'); }
    function _getSend()  { return document.getElementById('turkai-send'); }
    function _getTyping(){ return document.getElementById('turkai-typing'); }

    function _scrollToBottom() {
        var msgs = _getMsgs();
        msgs.scrollTop = msgs.scrollHeight;
    }

    function _appendMsg(text, isUser) {
        var msgs  = _getMsgs();
        var typing = _getTyping();
        var div = document.createElement('div');
        div.className = 'turkai-widget-msg ' + (isUser ? 'user' : 'ai');
        div.textContent = text;
        msgs.insertBefore(div, typing);
        _scrollToBottom();
    }

    function _setLoading(loading) {
        isLoading = loading;
        var send  = _getSend();
        var input = _getInput();
        var typing = _getTyping();
        send.disabled  = loading;
        input.disabled = loading;
        typing.style.display = loading ? 'inline-block' : 'none';
        _scrollToBottom();
    }

    // ── API call ──────────────────────────────────────────────────────────────
    function _sendMessage(text) {
        if (!text || isLoading) return;
        _appendMsg(text, true);
        history.push({ role: 'user', content: text });
        _setLoading(true);

        var payload = {
            message: text,
            language: LANGUAGE,
            sessionId: sessionId,
            history: history.slice(-10)
        };

        var headers = { 'Content-Type': 'application/json' };
        if (API_KEY) headers['Authorization'] = 'Bearer ' + API_KEY;

        fetch(API_BASE + '/api/chat', {
            method: 'POST',
            headers: headers,
            body: JSON.stringify(payload)
        })
        .then(function (res) {
            if (!res.ok) throw new Error('HTTP ' + res.status);
            return res.json();
        })
        .then(function (data) {
            sessionId = data.sessionId || sessionId;
            _appendMsg(data.reply, false);
            history.push({ role: 'assistant', content: data.reply });
        })
        .catch(function (err) {
            var errMsg = LANGUAGE === 'tr'
                ? 'Üzgünüm, bir hata oluştu. Lütfen tekrar deneyin.'
                : 'Sorry, something went wrong. Please try again.';
            _appendMsg(errMsg, false);
        })
        .finally(function () {
            _setLoading(false);
        });
    }

    // ── Event listeners ───────────────────────────────────────────────────────
    btn.addEventListener('click', function () {
        panel.classList.toggle('open');
        if (panel.classList.contains('open')) {
            _getInput().focus();
        }
    });

    panel.querySelector('.turkai-widget-close').addEventListener('click', function () {
        panel.classList.remove('open');
    });

    panel.addEventListener('click', function (e) {
        if (e.target.id === 'turkai-send') {
            var input = _getInput();
            var text = input.value.trim();
            if (text) { input.value = ''; _sendMessage(text); }
        }
    });

    panel.addEventListener('keydown', function (e) {
        if (e.target.id === 'turkai-input' && e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            var text = e.target.value.trim();
            if (text) { e.target.value = ''; _sendMessage(text); }
        }
    });
})();
