// Search and sort lists marked with data-list, including after Blazor navigation.
// Delegated handlers also work when enhanced navigation replaces page content.
(() => {
    const collator = new Intl.Collator(undefined, { sensitivity: 'base', numeric: true });

    function fieldText(item, field) {
        return item.getElementsByClassName(field)[0]?.textContent.trim() ?? '';
    }

    document.addEventListener('input', event => {
        if (!event.target.matches('[data-list] .search')) return;

        const container = event.target.closest('[data-list]');
        const list = container.querySelector('.list');
        if (!list) return;

        const fields = container.dataset.list.split(',').map(field => field.trim());
        const query = event.target.value.trim().toLocaleLowerCase();
        for (const item of list.children) {
            item.hidden = !fields.some(field =>
                fieldText(item, field).toLocaleLowerCase().includes(query));
        }
    });

    document.addEventListener('click', event => {
        const button = event.target.closest('[data-list] .sort[data-sort]');
        if (!button) return;

        const container = button.closest('[data-list]');
        const list = container.querySelector('.list');
        if (!list) return;

        const ascending = button.dataset.order !== 'asc';
        const items = Array.from(list.children);
        items.sort((a, b) => collator.compare(
            fieldText(a, button.dataset.sort),
            fieldText(b, button.dataset.sort)) * (ascending ? 1 : -1));
        list.append(...items);

        for (const sortButton of container.querySelectorAll('.sort[data-sort]')) {
            delete sortButton.dataset.order;
            sortButton.classList.remove('asc', 'desc');
        }
        button.dataset.order = ascending ? 'asc' : 'desc';
        button.classList.add(button.dataset.order);
        button.textContent = ascending ? 'Sort Z-A' : 'Sort A-Z';
    });
})();
