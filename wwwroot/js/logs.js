(() => {
  const serviceSelect = document.getElementById('serviceSelect');
  const sessionSelect = document.getElementById('sessionSelect');
  const keywordInput = document.getElementById('keywordInput');
  const statusInput = document.getElementById('statusInput');
  const fromInput = document.getElementById('fromInput');
  const toInput = document.getElementById('toInput');
  const pageInput = document.getElementById('pageInput');
  const pageSizeInput = document.getElementById('pageSizeInput');
  const queryBtn = document.getElementById('queryBtn');
  const streamBtn = document.getElementById('streamBtn');
  const logBody = document.getElementById('logTableBody');
  const pageInfo = document.getElementById('pageInfo');
  const prevBtn = document.getElementById('prevBtn');
  const nextBtn = document.getElementById('nextBtn');

  let es;

  function setSessions(service, data) {
    sessionSelect.innerHTML = '';
    const sessions = data[service] || [];
    for (const s of sessions) {
      const opt = document.createElement('option');
      opt.value = s; opt.textContent = s;
      sessionSelect.appendChild(opt);
    }
  }

  async function loadMetadata() {
    const res = await fetch('/metadata');
    const data = await res.json();
    serviceSelect.innerHTML = '';
    for (const svc of Object.keys(data)) {
      const opt = document.createElement('option');
      opt.value = svc; opt.textContent = svc;
      serviceSelect.appendChild(opt);
    }
    const first = serviceSelect.value;
    setSessions(first, data);
    serviceSelect.addEventListener('change', () => setSessions(serviceSelect.value, data));
  }

  function addRow(entry) {
    const tr = document.createElement('tr');
    const tdTs = document.createElement('td');
    const tdContent = document.createElement('td');
    tdTs.textContent = entry.timestamp || '';
    tdContent.textContent = entry.content || '';
    tr.appendChild(tdTs); tr.appendChild(tdContent);
    logBody.appendChild(tr);
  }

  function clearRows() {
    logBody.innerHTML = '';
  }

  function buildQuery() {
    const params = new URLSearchParams();
    params.set('serviceName', serviceSelect.value);
    params.set('sessionId', sessionSelect.value);
    if (keywordInput.value) params.set('q', keywordInput.value);
    if (statusInput.value) params.set('status', statusInput.value);
    if (fromInput.value) params.set('from', new Date(fromInput.value).toISOString());
    if (toInput.value) params.set('to', new Date(toInput.value).toISOString());
    params.set('page', pageInput.value || '1');
    params.set('pageSize', pageSizeInput.value || '100');
    return params.toString();
  }

  async function query() {
    if (es) { es.close(); es = null; }
    clearRows();
    const res = await fetch(`/payload-log?${buildQuery()}`);
    const data = await res.json();
    for (const e of data.entries || []) {
      addRow({ timestamp: e.timestamp, content: e.content });
    }
    const page = typeof data.page === 'number' ? data.page : parseInt(pageInput.value || '1');
    const pageSize = typeof data.pageSize === 'number' ? data.pageSize : parseInt(pageSizeInput.value || '100');
    const total = (typeof data.totalMatched === 'number') ? data.totalMatched : ((Array.isArray(data.entries) ? data.entries.length : 0));
    const totalPages = Math.max(1, Math.ceil((total || 0) / pageSize) || 1);
    pageInfo.textContent = `Page ${page}/${totalPages} | PageSize ${pageSize} | Total ${total}`;

    prevBtn.disabled = page <= 1 || totalPages <= 1;
    nextBtn.disabled = page >= totalPages || totalPages <= 1;
  }

  function stream() {
    clearRows();
    if (es) { es.close(); es = null; }
    es = new EventSource(`/payload-log/stream?${buildQuery()}`);
    prevBtn.disabled = true;
    nextBtn.disabled = true;
    es.onmessage = (ev) => {
      try {
        const obj = JSON.parse(ev.data);
        addRow(obj);
      } catch {}
    };
    es.onerror = () => {
      if (es) es.close();
      es = null;
    };
  }

  queryBtn.addEventListener('click', query);
  streamBtn.addEventListener('click', stream);
  prevBtn.addEventListener('click', () => {
    const p = Math.max(1, parseInt(pageInput.value || '1') - 1);
    pageInput.value = String(p);
    query();
  });
  nextBtn.addEventListener('click', () => {
    const p = parseInt(pageInput.value || '1') + 1;
    pageInput.value = String(p);
    query();
  });

  loadMetadata().then(query);
})();
