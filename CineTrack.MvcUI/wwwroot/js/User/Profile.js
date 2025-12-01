// 1. Liste Detayını Göster
function showListContents(listId) {
	var modal = new bootstrap.Modal(document.getElementById('listDetailModal'));
	modal.show();

	// ... (DOM element seçimleri aynı kalacak) ...
	document.getElementById('detailLoading').style.display = 'block';
	document.getElementById('listContentsContainer').innerHTML = '';
	document.getElementById('detailEmpty').style.display = 'none';
	document.getElementById('detailModalTitle').innerText = 'Yükleniyor...';
	document.getElementById('detailModalDesc').innerText = '';

	fetch(profileConfig.urls.getListDetails + listId)
		.then(res => res.json())
		.then(response => {
			document.getElementById('detailLoading').style.display = 'none';

			if (!response.success) {
				alert(response.message);
				modal.hide();
				return;
			}

			var list = response.data;
			document.getElementById('detailModalTitle').innerText = list.name;
			document.getElementById('detailModalDesc').innerText = list.description || '';

			if (!list.contents || list.contents.length === 0) {
				document.getElementById('detailEmpty').style.display = 'block';
				return;
			}

			// HATA BURADAYDI: @(Model.IsOwner...) yerine config'den alıyoruz
			var isOwner = profileConfig.isOwner;
			var container = document.getElementById('listContentsContainer');

			list.contents.forEach(item => {
				var deleteBtn = '';
				if (isOwner) {
					// String template içinde onclick düzeltmesi
					deleteBtn = `
                        <button onclick="removeContentFromList(${listId}, '${item.id}', this)" 
                                class="btn btn-sm btn-danger position-absolute top-0 end-0 m-2 shadow-sm" 
                                style="z-index: 10; border-radius: 50%; width: 30px; height: 30px; padding: 0; display: flex; align-items: center; justify-content: center;"
                                title="Listeden Çıkar">
                            <i class="bi bi-trash-fill" style="font-size: 0.8rem;"></i>
                        </button>
                    `;
				}

				// ... (Geri kalan HTML oluşturma kodu aynı) ...
				var cardHtml = `
                        <div class="col content-col">
                            <div class="card h-100 shadow-sm position-relative">
                                ${deleteBtn}
                                <img src="${item.coverUrl || '/images/placeholder.jpg'}" class="card-img-top" 
                                     style="aspect-ratio: 2/3; object-fit: cover;">
                                <div class="card-body p-2">
                                    <h6 class="card-title text-truncate small mb-1" title="${item.title}">${item.title}</h6>
                                    <a href="/Content/Detail?type=${item.contentType}&id=${item.id}" 
                                       class="btn btn-xs btn-outline-secondary w-100 stretched-link">Detay</a>
                                </div>
                            </div>
                        </div>
                    `;
				container.innerHTML += cardHtml;
			});
		})
		.catch(err => {
			console.error(err);
			alert("Hata oluştu.");
		});
}

// 2. İçerik Silme (Fonksiyon aynı kalabilir, endpoint'i hardcode bırakabilir veya configden alabilirsin)
function removeContentFromList(listId, contentId, btnElement) {
	if (event) {
		event.stopPropagation();
		event.preventDefault();
	}

	if (!confirm('Bu içeriği listeden çıkarmak istediğinize emin misiniz?')) return;

	var formData = new FormData();
	formData.append('listId', listId);
	formData.append('contentId', contentId);

	fetch('/UserList/RemoveContent', { // İstersen profileConfig.urls.removeContent kullanabilirsin
		method: 'POST',
		body: formData
	})
		.then(res => res.json())
		.then(data => {
			if (data.success) {
				// ... (Animasyon kodları aynı) ...
				var col = btnElement.closest('.content-col');
				col.style.transition = 'all 0.3s';
				col.style.opacity = '0';
				col.style.transform = 'scale(0.8)';
				setTimeout(() => {
					col.remove();
					if (document.querySelectorAll('.content-col').length === 0) {
						var detailEmpty = document.getElementById('detailEmpty');
						if (detailEmpty) detailEmpty.style.display = 'block';
					}
				}, 300);
			} else {
				alert(data.message);
			}
		});
}

// 3. Liste Silme
function deleteList(listId) {
	if (!confirm('Bu listeyi tamamen silmek istediğinize emin misiniz?')) return;
	var formData = new FormData();
	formData.append('id', listId);
	fetch('/UserList/DeleteList', { method: 'POST', body: formData })
		.then(res => res.json()).then(data => { if (data.success) location.reload(); else alert(data.message); });
}

// 4. Liste Düzenle Modalını Aç
function openEditListModal(id, name, desc) {
	document.getElementById('editListId').value = id;
	document.getElementById('editListName').value = name;
	document.getElementById('editListDesc').value = desc;
	new bootstrap.Modal(document.getElementById('editListModal')).show();
}

// 5. Liste Güncelleme
function submitUpdateList() {
	var id = document.getElementById('editListId').value;
	var name = document.getElementById('editListName').value;
	var desc = document.getElementById('editListDesc').value;
	var formData = new FormData();
	formData.append('id', id); formData.append('name', name); formData.append('description', desc);
	fetch('/UserList/UpdateList', { method: 'POST', body: formData })
		.then(res => res.json()).then(data => { if (data.success) location.reload(); else alert(data.message); });
}

// 6. Yeni Liste Oluştur
function submitCreateList() {
	var name = document.getElementById('listName').value;
	var desc = document.getElementById('listDesc').value;
	if (!name) { alert("Lütfen bir isim girin."); return; }
	var formData = new FormData();
	formData.append('name', name); formData.append('description', desc);
	fetch('/UserList/Create', { method: 'POST', body: formData })
		.then(res => res.json()).then(data => { alert(data.message); if (data.success) location.reload(); });
}