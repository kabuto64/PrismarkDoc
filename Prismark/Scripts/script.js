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

    processExternalLinks();
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

function isExternalLink(url) {
    const currentDomain = window.location.hostname;
    const linkDomain = new URL(url).hostname;
    return currentDomain !== linkDomain;
}

document.addEventListener('DOMContentLoaded', () => {
    const contentList = document.getElementById('content-list');
    createMenu(contentList);

    // 初期ページの読み込み
    const firstPage = Object.values(pages)[0];
    loadContent(firstPage);
});