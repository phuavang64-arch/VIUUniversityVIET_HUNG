/* ===================================================================
   VIU UNIVERSITY — front-end behaviour
   Nav (desktop dropdown + mobile), header search, admission assistant.

   Security rule applied throughout: anything that originates from the user
   or from the model is inserted with textContent / DOM APIs, never by
   concatenating a string into innerHTML.
   =================================================================== */
(function () {
    'use strict';

    /* ------------------------------------------------------------------
       1. Navigation
       ------------------------------------------------------------------ */
    function initNav() {
        var nav = document.getElementById('viuNav');
        var toggle = document.getElementById('viuNavToggle');
        if (!nav || !toggle) return;

        function closeMobileNav() {
            nav.classList.remove('show');
            toggle.setAttribute('aria-expanded', 'false');
        }

        toggle.addEventListener('click', function (e) {
            e.stopPropagation();
            var open = nav.classList.toggle('show');
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
        });

        // Click a toggle: open its menu, close the siblings.
        document.querySelectorAll('[data-viu-dropdown]').forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                e.stopPropagation();
                var menu = document.getElementById(btn.getAttribute('data-viu-dropdown'));
                if (!menu) return;

                var willOpen = !menu.classList.contains('show');
                document.querySelectorAll('.viu-dropdown__menu.show').forEach(function (m) {
                    m.classList.remove('show');
                });
                document.querySelectorAll('[data-viu-dropdown][aria-expanded="true"]').forEach(function (b) {
                    b.setAttribute('aria-expanded', 'false');
                });

                if (willOpen) {
                    menu.classList.add('show');
                    btn.setAttribute('aria-expanded', 'true');
                }
            });
        });

        // Click outside closes dropdowns; on mobile it also closes the whole menu.
        document.addEventListener('click', function (e) {
            if (!e.target.closest('.viu-dropdown')) {
                document.querySelectorAll('.viu-dropdown__menu.show').forEach(function (m) {
                    m.classList.remove('show');
                });
                document.querySelectorAll('[data-viu-dropdown][aria-expanded="true"]').forEach(function (b) {
                    b.setAttribute('aria-expanded', 'false');
                });
            }
            if (window.innerWidth < 992 && nav.classList.contains('show') && !e.target.closest('.viu-header')) {
                closeMobileNav();
            }
        });

        document.addEventListener('keydown', function (e) {
            if (e.key !== 'Escape') return;
            document.querySelectorAll('.viu-dropdown__menu.show').forEach(function (m) {
                m.classList.remove('show');
            });
            document.querySelectorAll('[data-viu-dropdown][aria-expanded="true"]').forEach(function (b) {
                b.setAttribute('aria-expanded', 'false');
            });
            closeMobileNav();
        });
    }

    /* ------------------------------------------------------------------
       2. Header search
       ------------------------------------------------------------------ */
    function initSearch() {
        var input = document.getElementById('globalSearchInput');
        var panel = document.getElementById('searchResults');
        var results = document.getElementById('searchResultsList');
        var hint = document.getElementById('searchHint');
        var seeAll = document.getElementById('searchSeeAll');
        if (!input || !results) return;

        var timer = null;
        var lastQuery = '';

        function clear() {
            results.replaceChildren();
            if (panel) panel.hidden = true;
            if (hint) hint.hidden = false;
            if (seeAll) seeAll.hidden = true;
        }

        function emptyState(term) {
            var wrap = document.createElement('div');
            wrap.className = 'p-3 text-muted small';
            // term is user input -> textContent, never innerHTML.
            wrap.append('Không tìm thấy kết quả nào cho “', term, '”.');
            return wrap;
        }

        function resultRow(item) {
            var a = document.createElement('a');
            a.className = 'search-result';
            a.href = item.url;

            var left = document.createElement('span');
            var title = document.createElement('strong');
            title.textContent = item.title || '';
            var type = document.createElement('span');
            type.className = 'search-result__type';
            type.textContent = item.type || '';
            left.append(title, type);

            var date = document.createElement('span');
            date.className = 'search-result__date';
            date.textContent = item.date || '';

            a.append(left, date);
            return a;
        }

        input.addEventListener('input', function () {
            var term = input.value.trim();
            clearTimeout(timer);
            if (term.length < 2) { clear(); return; }
            timer = setTimeout(function () { load(term); }, 250);
        });

        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && input.value.trim()) {
                e.preventDefault();
                window.location.href = '/tim-kiem?q=' + encodeURIComponent(input.value.trim());
            }
        });

        function load(term) {
            fetch('/api/search?q=' + encodeURIComponent(term), { headers: { 'Accept': 'application/json' } })
                .then(function (r) { return r.ok ? r.json() : null; })
                .then(function (data) {
                    if (!data) { clear(); return; }
                    lastQuery = term;

                    var items = []
                        .concat(data.majors || [], data.news || [], data.announcements || [], data.events || []);

                    results.replaceChildren();

                    if (items.length === 0) {
                        results.appendChild(emptyState(term));
                    } else {
                        items.slice(0, 8).forEach(function (i) { results.appendChild(resultRow(i)); });
                    }

                    if (hint) hint.hidden = true;
                    if (panel) panel.hidden = false;
                    if (seeAll) {
                        seeAll.href = '/tim-kiem?q=' + encodeURIComponent(term);
                        seeAll.hidden = false;
                    }
                })
                .catch(function () {
                    results.replaceChildren();
                    var p = document.createElement('div');
                    p.className = 'p-3 text-muted small';
                    p.textContent = 'Không tải được kết quả tìm kiếm.';
                    results.appendChild(p);
                });
        }

        // Close when clicking outside the search box.
        document.addEventListener('click', function (e) {
            if (!e.target.closest('#globalSearch')) clear();
        });
    }

    /* ------------------------------------------------------------------
       3. Admission assistant
       ------------------------------------------------------------------ */
    function initChat() {
        var cfg = window.VIU_CHAT;
        if (!cfg) return;

        var panel = document.getElementById('viuChat');
        var toggle = document.getElementById('viuChatToggle');
        var closeBtn = document.getElementById('viuChatClose');
        var form = document.getElementById('viuChatForm');
        var input = document.getElementById('viuChatInput');
        var sendBtn = document.getElementById('viuChatSend');
        var log = document.getElementById('viuChatLog');
        var chips = document.getElementById('viuChatChips');
        var statusEl = document.getElementById('viuChatStatus');

        if (!panel || !toggle || !form || !input || !log) return;

        var history = [];   // in-session conversation turns
        var busy = false;

        var GREETING = '<p>Xin chào! Mình là <strong>Trợ lý AI Việt – Hung</strong>. Bạn có thể trò chuyện, thảo luận hoặc đặt bất kỳ câu hỏi nào về trường cũng như các chủ đề bạn quan tâm nhé!</p>';

        function setStatus(text, state) {
            if (!statusEl) return;
            statusEl.textContent = text;
            statusEl.setAttribute('data-state', state);
        }

        function open() {
            panel.classList.add('is-open');
            // Đánh dấu trạng thái mở trên <body> để CSS ẩn nút tròn: nút tròn nằm
            // TRƯỚC panel trong DOM nên không dùng được selector "~" của CSS.
            // Đồng thời khoá cuộn trang nền khi hộp thoại chiếm gần hết màn hình.
            document.body.classList.add('viu-chat-open');
            toggle.setAttribute('aria-expanded', 'true');
            toggle.setAttribute('aria-label', 'Đóng trợ lý tuyển sinh');
            if (!log.childElementCount) {
                botSay(GREETING, cfg.configured ? null : 'Hiện chưa cấu hình dịch vụ AI');
                renderChips([
                    'Trường có những ngành đào tạo nào?',
                    'Tư vấn chọn ngành phù hợp',
                    'Học phí và học bổng',
                    'Trò chuyện cùng AI'
                ]);
            }
            input.focus();
        }

        function shut() {
            panel.classList.remove('is-open');
            document.body.classList.remove('viu-chat-open');
            toggle.setAttribute('aria-expanded', 'false');
            toggle.setAttribute('aria-label', 'Mở trợ lý tuyển sinh');
            toggle.focus();
        }

        toggle.addEventListener('click', function () {
            panel.classList.contains('is-open') ? shut() : open();
        });
        if (closeBtn) closeBtn.addEventListener('click', shut);

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && panel.classList.contains('is-open')) shut();
        });

        function botSay(html, note) {
            var wrap = document.createElement('div');
            wrap.className = 'viu-chat__msg viu-chat__msg--bot';
            // Model output is sanitized server-side; insert as HTML on purpose.
            wrap.innerHTML = html;

            if (note) {
                var small = document.createElement('div');
                small.className = 'viu-chat__note';
                small.textContent = note;
                wrap.appendChild(small);
            }
            log.appendChild(wrap);
            log.scrollTop = log.scrollHeight;
        }

        function userSay(text) {
            var wrap = document.createElement('div');
            wrap.className = 'viu-chat__msg viu-chat__msg--user';
            // User text: always plain text.
            wrap.textContent = text;
            log.appendChild(wrap);
            log.scrollTop = log.scrollHeight;
        }

        function showTyping() {
            var t = document.createElement('div');
            t.className = 'viu-chat__msg viu-chat__msg--bot';
            t.id = 'viuTyping';
            t.innerHTML = '<span class="viu-chat__typing"><span></span><span></span><span></span></span>';
            log.appendChild(t);
            log.scrollTop = log.scrollHeight;
        }

        function hideTyping() {
            var t = document.getElementById('viuTyping');
            if (t) t.remove();
        }

        function renderChips(items) {
            if (!chips) return;
            chips.replaceChildren();
            (items || []).slice(0, 4).forEach(function (q) {
                var b = document.createElement('button');
                b.type = 'button';
                b.className = 'viu-chip';
                b.textContent = q;
                b.addEventListener('click', function () { ask(q); });
                chips.appendChild(b);
            });
        }

        function ask(question) {
            question = (question || '').trim();
            if (!question || busy) return;

            busy = true;
            sendBtn.disabled = true;
            input.value = '';
            userSay(question);
            if (chips) chips.replaceChildren();
            showTyping();
            setStatus(cfg.configured ? 'Đang suy nghĩ...' : 'Đang tra cứu dữ liệu...', 'online');

            fetch(cfg.endpoint, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                body: JSON.stringify({ message: question, history: history.slice(-10) })
            })
                .then(function (r) {
                    if (!r.ok) throw new Error('http ' + r.status);
                    return r.json();
                })
                .then(function (data) {
                    hideTyping();

                    var answer = data.reply || '<p>Không nhận được nội dung phản hồi.</p>';
                    var note = null;

                    if (data.source === 'Unavailable') {
                        // Phân biệt "chưa cấu hình" với "lỗi tạm thời" để
                        // không khiến người dùng tưởng hệ thống đang hỏng.
                        if (!cfg.configured) {
                            setStatus('Chế độ tra cứu dữ liệu', 'offline');
                        } else {
                            setStatus('Tạm thời không phản hồi', 'error');
                        }
                        note = data.sourceNote || 'Không lấy được dữ liệu';
                    } else {
                        setStatus(cfg.configured ? 'AI Trực tuyến' : 'Chế độ tra cứu dữ liệu', 'online');
                        note = data.sourceNote;
                    }

                    botSay(answer, note);
                    history.push({ role: 'user', content: question });
                    history.push({ role: 'assistant', content: stripHtml(answer) });

                    if (data.suggestions) renderChips(data.suggestions);
                    if (data.links && data.links.length) renderLinks(data.links);
                })
                .catch(function () {
                    hideTyping();
                    setStatus('Lỗi kết nối', 'error');
                    botSay('<p>Không gửi được câu hỏi. Bạn vui lòng kiểm tra kết nối và thử lại.</p>',
                        'Bấm để gửi lại');
                    renderChips(['Thử lại câu hỏi vừa rồi', 'Liên hệ hotline tuyển sinh']);
                    history.push({ role: 'user', content: question });
                    var retry = chips && chips.lastElementChild;
                    if (retry) {
                        retry.addEventListener('click', function () { ask(question); }, { once: true });
                    }
                })
                .then(function () {
                    busy = false;
                    sendBtn.disabled = false;
                    input.focus();
                });
        }

        function renderLinks(links) {
            var box = document.createElement('div');
            box.className = 'viu-chat__links';

            links.forEach(function (l) {
                var a = document.createElement('a');
                a.className = 'btn-viu btn-viu-outline btn-viu-sm';
                a.href = l.url;
                a.textContent = l.label;
                box.appendChild(a);
            });

            log.appendChild(box);
            log.scrollTop = log.scrollHeight;
        }

        function stripHtml(html) {
            var d = document.createElement('div');
            d.innerHTML = html;
            return (d.textContent || '').trim();
        }

        form.addEventListener('submit', function (e) {
            e.preventDefault();
            ask(input.value);
        });
    }

    /* ------------------------------------------------------------------
       4. Hiệu ứng xuất hiện khi cuộn tới
       ------------------------------------------------------------------
       Chỉ chạy nếu trình duyệt hỗ trợ IntersectionObserver. Nếu không có (hoặc
       người dùng bật prefers-reduced-motion) thì mọi khối hiện ngay, không cần
       làm gì — nội dung không bao giờ bị ẩn vĩnh viễn vì hiệu ứng không chạy. */
    function initReveal() {
        var items = document.querySelectorAll('.viu-reveal');
        if (!items.length) return;

        var reduceMotion = window.matchMedia
            && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

        if (reduceMotion || !('IntersectionObserver' in window)) {
            items.forEach(function (el) { el.classList.add('is-visible'); });
            return;
        }

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) return;
                entry.target.classList.add('is-visible');
                observer.unobserve(entry.target);
            });
        }, { rootMargin: '0px 0px -8% 0px', threshold: 0.08 });

        items.forEach(function (el) { observer.observe(el); });
    }

    /* ------------------------------------------------------------------
       Boot
       ------------------------------------------------------------------ */
    document.addEventListener('DOMContentLoaded', function () {
        initNav();
        initSearch();
        initChat();
        initReveal();
    });
})();