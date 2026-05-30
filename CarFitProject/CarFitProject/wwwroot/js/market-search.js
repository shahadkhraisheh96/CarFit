document.addEventListener('DOMContentLoaded', function () {
    const keywordInput = document.getElementById('keywordSearch');
    const brandSelect = document.getElementById('brandSelect');
    const priceInput = document.getElementById('priceCeiling');
    const listings = document.querySelectorAll('.listing-node');

    // 1. Dynamic Dropdown Auto-Generation Engine Loop
    const brandsSet = new Set();
    listings.forEach(node => {
        const make = node.getAttribute('data-make');
        if (make) brandsSet.add(make);
    });

    // Populate drop down options with array contents smoothly
    brandsSet.forEach(brandName => {
        const option = document.createElement('option');
        option.value = brandName;
        option.textContent = brandName.toUpperCase();
        brandSelect.appendChild(option);
    });

    // 2. High-Performance Execution Evaluation Logic
    function runSearchPipeline() {
        const searchKeyword = keywordInput.value.toLowerCase().trim();
        const selectedBrand = brandSelect.value.toLowerCase();
        const priceLimit = parseFloat(priceInput.value) || Infinity;

        listings.forEach(listing => {
            const make = listing.getAttribute('data-make') || "";
            const model = listing.getAttribute('data-model') || "";
            const trim = listing.getAttribute('data-trim') || "";
            const price = parseFloat(listing.getAttribute('data-price')) || 0;

            // Property array conditional comparison matrix evaluation
            const keywordMatch = make.includes(searchKeyword) ||
                model.includes(searchKeyword) ||
                trim.includes(searchKeyword);
            const brandMatch = !selectedBrand || make === selectedBrand;
            const priceMatch = price <= priceLimit;

            // Trigger show/hide animations depending on matching logic conditions
            if (keywordMatch && brandMatch && priceMatch) {
                listing.style.display = "";
            } else {
                listing.style.display = "none";
            }
        });
    }

    // 3. Debounce Delay Utility Implementation
    function debounce(func, delay) {
        let timeoutTimer;
        return function (...args) {
            clearTimeout(timeoutTimer);
            timeoutTimer = setTimeout(() => func.apply(this, args), delay);
        };
    }

    // Bind event hooks to tracking elements safely
    keywordInput.addEventListener('input', debounce(runSearchPipeline, 200));
    brandSelect.addEventListener('change', runSearchPipeline);
    priceInput.addEventListener('input', debounce(runSearchPipeline, 200));
});
// Append this inside the document.body context loader lifecycle loop in market-search.js
const terms = document.querySelectorAll('.glossary-term');

terms.forEach(termElement => {
    let tooltipBox = null;

    termElement.addEventListener('mouseenter', async function (e) {
        const rawTerm = this.getAttribute('data-term');

        // 1. Initialize floating viewport component template layer
        tooltipBox = document.createElement('div');
        tooltipBox.className = 'glossary-tooltip-panel p-3 shadow-lg position-absolute rounded';
        tooltipBox.style.cssText = `
            z-index: 1050;
            width: 280px;
            background: #1e293b;
            border: 1px solid rgba(255,255,255,0.1);
            color: #f8fafc;
            font-size: 0.8rem;
            pointer-events: none;
            opacity: 0;
            transition: opacity 0.15s ease;
        `;
        tooltipBox.innerHTML = `<div class="text-muted small"><i class="fa fa-spinner fa-spin me-1"></i> Decoding ورقة الفحص...</div>`;
        document.body.appendChild(tooltipBox);

        // Position the box right over the hovered node coordinates elements
        const bounds = this.getBoundingClientRect();
        tooltipBox.style.left = `${bounds.left + window.scrollX}px`;
        tooltipBox.style.top = `${bounds.top + window.scrollY - tooltipBox.offsetHeight - 10}px`;
        tooltipBox.style.opacity = '1';

        try {
            // 2. Fetch parameters live from database controller model contexts API endpoints
            const response = await fetch(`/Inventory/GetTermExplanation?term=${encodeURIComponent(rawTerm)}`);
            if (response.ok) {
                const data = await response.json();

                // Color switch properties matrix evaluating severity
                let statusBadgeColor = data.severity === 'Critical' ? '#ef4444' : '#10b981';

                tooltipBox.innerHTML = `
                    <div class="d-flex justify-content-between align-items-center mb-2 border-bottom border-secondary pb-1">
                        <strong class="text-white">Bilingual Definition</strong>
                        <span class="badge small" style="background-color: ${statusBadgeColor}">${data.severity} Risk</span>
                    </div>
                    <div class="mb-1 text-end text-warning font-sans" dir="rtl">${data.ar}</div>
                    <div class="text-muted small leading-normal">${data.en}</div>
                `;
            } else {
                tooltipBox.innerHTML = `<span class="text-danger"><i class="fa fa-circle-exclamation me-1"></i> Definition unindexed.</span>`;
            }
        } catch (err) {
            tooltipBox.innerHTML = `<span class="text-muted">Failed to stream definition matrix.</span>`;
        }
    });

    termElement.addEventListener('mouseleave', function () {
        if (tooltipBox) {
            tooltipBox.remove();
            tooltipBox = null;
        }
    });
});