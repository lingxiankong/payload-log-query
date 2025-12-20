(() => {
  const serviceSelect = document.getElementById('serviceSelect');
  const sessionSelect = document.getElementById('sessionSelect');
  const keywordInput = document.getElementById('keywordInput');
  const statusInput = document.getElementById('statusInput');
  const fromInput = document.getElementById('fromInput');
  const toInput = document.getElementById('toInput');

  const limitInput = document.getElementById('limitInput');
  const queryBtn = document.getElementById('queryBtn');
  const logBody = document.getElementById('logTableBody');
  const loadMoreBtn = document.getElementById('loadMoreBtn');

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
    try {
      const res = await fetch('/metadata?_' + Date.now());
      if (!res.ok) throw new Error('Failed to load metadata');
      const data = await res.json();

      serviceSelect.innerHTML = '';
      const services = Object.keys(data);
      if (services.length === 0) {
          const opt = document.createElement('option');
          opt.textContent = "No logs found";
          serviceSelect.appendChild(opt);
          sessionSelect.innerHTML = '';
          return;
      }

      for (const svc of services) {
        const opt = document.createElement('option');
        opt.value = svc; opt.textContent = svc;
        serviceSelect.appendChild(opt);
      }

      serviceSelect.selectedIndex = 0;
      setSessions(serviceSelect.value, data);

      serviceSelect.addEventListener('change', () => setSessions(serviceSelect.value, data));
    } catch (err) {
      console.error(err);
      alert('Error loading metadata: ' + err.message);
    }
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

  let lastTimestamp = null;

  async function query() {
    clearRows();
    if (es) { es.close(); es = null; }

    // Check required fields
    if (!serviceSelect.value || !sessionSelect.value) {
        alert('Please select Service and Session');
        return;
    }

    startStream(null);
  }

  function loadMore() {
      if (!lastTimestamp) return;
      // Pass true to excludeFrom to avoid duplication of the boundary item
      startStream(lastTimestamp, true);
  }

  function startStream(fromCursor, excludeFrom) {
      if (es) { es.close(); es = null; }

      const params = new URLSearchParams();
      params.set('serviceName', serviceSelect.value);
      params.set('sessionId', sessionSelect.value);
      if (keywordInput.value) params.set('q', keywordInput.value);
      if (statusInput.value) params.set('status', statusInput.value);

      // If loading more, override 'From' with cursor.
      // Else use User Input, appending 'Z' to treat it as UTC literal to match log time.
      if (fromCursor) {
          params.set('from', fromCursor);
          if (excludeFrom) {
            params.set('excludeFrom', 'true');
          }
      } else if (fromInput.value) {
          params.set('from', fromInput.value + 'Z');
      }

      if (toInput.value) params.set('to', toInput.value + 'Z');

      // Map Limit
      const limit = limitInput.value || '100';
      params.set('limit', limit);

      es = new EventSource(`/payload-log/stream?${params.toString()}`);

      loadMoreBtn.style.display = 'none';
      loadMoreBtn.disabled = true;

      es.onmessage = (ev) => {
        try {
          const obj = JSON.parse(ev.data);
          addRow(obj);
          if (obj.timestamp) lastTimestamp = obj.timestamp;
        } catch {}
      };

      es.onerror = () => {
        if (es) es.close();
        es = null;
        // Stream ended (or failed). Show Load More button if we have data.
        if (logBody.children.length > 0) {
            loadMoreBtn.style.display = 'block';
            loadMoreBtn.disabled = false;
            loadMoreBtn.textContent = "Load More";
        }
      };
  }

  queryBtn.addEventListener('click', query);
  loadMoreBtn.addEventListener('click', loadMore);

  loadMetadata();
})();
