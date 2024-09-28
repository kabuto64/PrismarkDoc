// PAGES_PLACEHOLDER
// APPCONFIG_PLACEHOLDER

function createMenu(container) {
    const menuStructure = {};

    Object.entries(pages).forEach(([fileName, pageInfo]) => {
        const parts = pageInfo.Number.split('-');
        let currentLevel = menuStructure;

        parts.forEach((part, index) => {
            if (!currentLevel[part]) {
                currentLevel[part] = { subItems: {} };
            }
            if (index === parts.length - 1) {
                currentLevel[part].pageInfo = pageInfo;
            }
            currentLevel = currentLevel[part].subItems;
        });
    });

    function createMenuItems(structure, container, level = 0) {
        const sortedKeys = Object.keys(structure).sort((a, b) => {
            const numA = parseInt(a.split('-').pop());
            const numB = parseInt(b.split('-').pop());
            return numA - numB;
        });

        sortedKeys.forEach(key => {
            const item = structure[key];
            if (item.pageInfo) {
                const link = document.createElement('a');
                link.href = `#${getPageId(item.pageInfo)}`;
                link.className = `toc-link level-${level}`;
                link.textContent = appConfig.ShowNumbers
                    ? `${item.pageInfo.Number} ${item.pageInfo.Title}`
                    : item.pageInfo.Title;
                link.onclick = (e) => {
                    e.preventDefault();
                    loadContent(item.pageInfo);
                    scrollToPageTop(item.pageInfo);
                };
                container.appendChild(link);
            }
            if (Object.keys(item.subItems).length > 0) {
                createMenuItems(item.subItems, container, level + 1);
            }
        });
    }

    createMenuItems(menuStructure, container);
}

function getPageId(pageInfo) {
    return `page-${pageInfo.Number.replace(/\./g, '-')}`;
}

function loadContent(pageInfo) {
    const content = document.getElementById('content');
    const pageId = getPageId(pageInfo);
    const pageTitle = appConfig.ShowNumbers
        ? `${pageInfo.Number} ${pageInfo.Title}`
        : pageInfo.Title;

    content.innerHTML = `${pageInfo.Content}`;

    // h1にIDを設定する。
    // h1が無かったら、mdファイル名でh1タグを先頭に生成する。
    const h1 = content.querySelector('h1');
    if (h1) {
        h1.id = pageId;
    } else {
        content.innerHTML = `<h1 id="${pageId}">${pageTitle}</h1>${pageInfo.Content}`;
    }

    generateTOC();

    //processExternalLinks();
    addCodeLanguageLabel();
    hljs.highlightAll();
    hljs.initLineNumbersOnLoad();
}

function generateTOC() {
    const toc = document.getElementById('toc');
    toc.innerHTML = '<h3>Index</h3>';
    const headers = content.querySelectorAll('h1, h2, h3, h4, h5, h6');
    headers.forEach((header, index) => {
        const link = document.createElement('a');
        link.href = `#${header.id}`;
        link.textContent = header.textContent;
        link.className = `toc-link toc-${header.tagName.toLowerCase()}`;
        link.onclick = (e) => {
            // ここでイベントをキャンセルしないことで、デフォルトのジャンプ動作を許可します
            setTimeout(removeHashFromURL, 0);
        };
        toc.appendChild(link);
    });
}

function scrollToPageTop(pageInfo) {
    const pageId = getPageId(pageInfo);
    const element = document.getElementById(pageId);
    if (element) {
        element.scrollIntoView({ behavior: 'auto' });
    }
    removeHashFromURL();
}

function removeHashFromURL() {
    history.replaceState(null, document.title, window.location.pathname + window.location.search);
}

function addCodeLanguageLabel() {
    document.querySelectorAll('pre code').forEach(function (block) {
        const languageClass = Array.from(block.classList).find(cls => cls.startsWith('language-'));
        if (languageClass) {
            const language = languageClass.replace('language-', '');
            const label = document.createElement('span');
            label.className = 'language-label';
            label.textContent = language;
            block.parentNode.insertBefore(label, block);
        }
    });
}

/**
 * HTML内の外部リンクを検知し、装飾する
 * */
function processExternalLinks() {
    const links = document.querySelectorAll('a');
    links.forEach(link => {
        if (isExternalLink(link.href)) {
            const externalLinkDiv = document.createElement('div');
            const linkTitle = document.createElement('p');
            const linkURL = document.createElement('p');

            linkTitle.className = 'exlink-title';
            linkURL.className = 'exlink-url';

            const favicon = `<img src="content/img/faviconV2.png" width="14" height="14" />`;
            if (link.innerText) {
                linkTitle.innerText = link.innerText;
                linkURL.innerHTML = `${favicon}${link.href}`
            } else {
                linkTitle.innerText = link.href;
                linkURL.innerHTML = `${favicon}${link.href}`
            }
            externalLinkDiv.className = 'exlink';
            externalLinkDiv.appendChild(linkTitle);
            externalLinkDiv.appendChild(linkURL);

            link.innerHTML = '';
            link.target = '_brank';
            link.appendChild(externalLinkDiv);
        }
    });
}

// これが外部リンクかどうかを判定する
function isExternalLink(url) {
    const currentDomain = window.location.hostname;
    const linkDomain = new URL(url).hostname;
    return currentDomain !== linkDomain;
}

// pagesオブジェクト内を検索する
function searchContents(data, searchTerm) {
    // 検索語を小文字に変換（大文字小文字を区別しない検索のため）
    const lowerSearchTerm = searchTerm.toLowerCase();

    const matchingPages = Object.entries(data).filter(([key, page]) =>
        stripHtml(page.Content).toLowerCase().includes(lowerSearchTerm)
    );

    return matchingPages.map(([key, page]) => ({
        Key: key,
        Title: page.Title,
        Number: page.Number,
        Content: page.Content
    }));
}
// 文字列内から、検索した文字列の前後指定文字数分抽出する
function extractContext(text, searchTerm, contextLength = 100) {
    const lowerText = text.toLowerCase();
    const lowerSearchTerm = searchTerm.toLowerCase();
    const index = lowerText.indexOf(lowerSearchTerm);
    if (index === -1) return '';

    let start = Math.max(0, index - contextLength);
    let end = Math.min(text.length, index + searchTerm.length + contextLength);

    let result = text.slice(start, end);

    if (start > 0) result = '...' + result;
    if (end < text.length) result = result + '...';

    // 検索語にハイライトを付与
    const highlightedTerm = `<mark>${escapeHtml(text.slice(index, index + searchTerm.length))}</mark>`;
    const regex = new RegExp(escapeHtml(searchTerm), 'gi');
    return result.replace(regex, highlightedTerm);
}
// HTML文字列からHTMLタグを消す
function stripHtml(html) {
    const doc = new DOMParser().parseFromString(html, 'text/html');
    return doc.body.textContent || "";
}
// HTML文字列のエスケープ
function escapeHtml(text) {
    const escapeMap = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, function (m) { return escapeMap[m]; });
}

// 検索を実行し、検索結果ページを表示する。
function executeSearch(event) {
    const searchText = escapeHtml(document.getElementById('txtSearch').value);
    if (searchText) {
        // すべてのページから語句を検索
        const resultPages = searchContents(pages, searchText);
        let searchResultElement = document.createElement("div");
        let h1 = document.createElement("h1");
        h1.innerHTML = "検索結果";
        searchResultElement.appendChild(h1);

        let info = document.createElement("p");

        if (resultPages.length > 0) {
            info.innerHTML = `検索語句「${searchText}」の結果：${resultPages.length}件見つかりました。`;
            searchResultElement.appendChild(info);
            resultPages.forEach(page => {
                let resultItem = document.createElement("a");
                resultItem.innerHTML = `
<div class="note" style="margin:10px;">
    <h2>${page.Title}</h2>
    <p>${extractContext(escapeHtml(stripHtml(page.Content)), searchText)}</p>
</div>
`;
                resultItem.href = `#${getPageId(page)}`;
                resultItem.onclick = (e) => {
                    e.preventDefault();
                    loadContent(page);
                    scrollToPageTop(page);
                };
                searchResultElement.appendChild(resultItem);
            })
        } else {
            searchResultElement.appendChild(info);
            info.innerHTML = `検索ワード「${searchText}」を含むページは見つかりませんでした。`;
        }
        const content = document.getElementById('content');
        content.innerHTML = '';
        content.appendChild(searchResultElement);
        generateTOC();
    }
}

document.addEventListener('DOMContentLoaded', () => {
    const headerTitle = document.getElementById('title')
    document.title = appConfig.DocumentTitle;
    headerTitle.innerText = appConfig.DocumentTitle;

    const contentList = document.getElementById('content-list');
    createMenu(contentList);

    // 初期ページの読み込み
    const firstPage = Object.values(pages)[0];
    loadContent(firstPage);

    const searchButton = document.getElementById('btnSearch');
    searchButton.addEventListener('click', (e) => {
        executeSearch(e);
    })
    const searchText = document.getElementById('txtSearch');
    searchText.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            executeSearch(e);
        }
    })
});