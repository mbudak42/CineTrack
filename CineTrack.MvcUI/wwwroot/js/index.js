let currentActivityId = 0;
let commentsModalInstance = null; // Modal örneğini hafızada tutmak için

// 1. Beğeni İşlemi
function toggleLike(btn, activityId) {
	const icon = btn.querySelector('i');
	const span = btn.querySelector('span');
	let count = parseInt(span.innerText);

	const isLiked = icon.classList.contains('bi-heart-fill');

	// UI Anında Tepki (Optimistic Update)
	if (isLiked) {
		icon.classList.replace('bi-heart-fill', 'bi-heart');
		btn.classList.replace('text-danger', 'text-muted');
		span.innerText = Math.max(0, count - 1);
	} else {
		icon.classList.replace('bi-heart', 'bi-heart-fill');
		btn.classList.replace('text-muted', 'text-danger');
		span.innerText = count + 1;
	}

	// Backend İsteği
	fetch(`/Feed/ToggleLike/${activityId}`, { method: 'POST' })
		.then(res => {
			if (!res.ok) throw new Error("API Hatası");
			return res.json();
		})
		.then(data => {
			// Başarılı, ek işlem gerekmez.
		})
		.catch(err => {
			console.error('Beğeni işlemi başarısız:', err);
			// Hata olursa işlemi geri al (Rollback UI)
			if (isLiked) {
				icon.classList.replace('bi-heart', 'bi-heart-fill');
				btn.classList.replace('text-muted', 'text-danger');
				span.innerText = count;
			} else {
				icon.classList.replace('bi-heart-fill', 'bi-heart');
				btn.classList.replace('text-danger', 'text-muted');
				span.innerText = count;
			}
			alert("İşlem gerçekleştirilemedi. Giriş yaptığınızdan emin olun.");
		});
}

// 2. Yorumları Aç
function openCommentsModal(activityId) {
	currentActivityId = activityId;

	// Modal elementini bul
	const modalEl = document.getElementById('commentsModal');

	// Varsa mevcut modalı kullan, yoksa oluştur (Bootstrap 5 Best Practice)
	if (!commentsModalInstance) {
		commentsModalInstance = new bootstrap.Modal(modalEl);
	}

	const container = document.getElementById('commentsList');
	container.innerHTML = '<div class="text-center py-3"><div class="spinner-border text-primary"></div></div>';

	commentsModalInstance.show();

	fetch(`/Feed/GetComments/${activityId}`)
		.then(res => {
			if (!res.ok) throw new Error("Yorumlar yüklenemedi");
			return res.json();
		})
		.then(data => {
			container.innerHTML = '';
			if (!data || data.length === 0) {
				container.innerHTML = '<p class="text-muted text-center py-3">Henüz yorum yok. İlk yorumu sen yap!</p>';
				return;
			}

			data.forEach(c => {
				// Tarih formatlama
				const dateStr = new Date(c.createdAt).toLocaleString('tr-TR', { day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' });

				const html = `
                    <div class="d-flex gap-2 mb-2">
                        <img src="${c.userAvatar || '/images/none.png'}" class="rounded-circle border" style="width:32px;height:32px;object-fit:cover;">
                        <div class="bg-light p-2 rounded flex-grow-1">
                            <div class="d-flex justify-content-between align-items-center">
                                <span class="fw-bold small">${c.username}</span>
                                <span class="text-muted" style="font-size:0.7rem">${dateStr}</span>
                            </div>
                            <div class="text-dark small mt-1">${c.text}</div>
                        </div>
                    </div>
                `;
				container.innerHTML += html;
			});
		})
		.catch(err => {
			console.error(err);
			container.innerHTML = '<p class="text-danger text-center">Yorumlar yüklenirken bir hata oluştu.</p>';
		});
}

// 3. Yorum Gönder
function submitComment() {
	const input = document.getElementById('commentInput');
	const text = input.value;
	if (!text.trim()) return;

	const formData = new FormData();
	formData.append('text', text);

	// Butonu pasife al (Çift tıklamayı önle)
	const btn = document.querySelector('#commentsModal .modal-footer .btn-primary');
	const originalText = btn.innerText;
	btn.disabled = true;
	btn.innerText = "Gönderiliyor...";

	fetch(`/Feed/AddComment/${currentActivityId}`, {
		method: 'POST',
		body: formData
	})
		.then(res => {
			if (!res.ok) throw new Error("Yorum gönderilemedi");
			return res.json(); // JSON parsing hatasını önlemek için kontrol
		})
		.then(newComment => {
			input.value = '';
			const container = document.getElementById('commentsList');
			if (container.innerText.includes('Henüz yorum yok')) container.innerHTML = '';

			// Yeni yorumu ekle (Fake avatar ve isim yerine dönen veriyi kullanıyoruz)
			// Eğer newComment boş gelirse (API hatası vb.) manuel doldurma yapabilirsin ama API düzgünse gerek yok.
			const html = `
            <div class="d-flex gap-2 mb-2">
                <img src="${newComment.userAvatar || '/images/none.png'}" class="rounded-circle border" style="width:32px;height:32px;object-fit:cover;">
                <div class="bg-light p-2 rounded flex-grow-1">
                    <div class="fw-bold small">${newComment.username || 'Ben'}</div>
                    <div class="text-dark small mt-1">${newComment.text || text}</div>
                    <div class="text-muted" style="font-size:0.7rem">Şimdi</div>
                </div>
            </div>
        `;
			container.innerHTML += html;

			// Scroll'u en alta indir
			// container.scrollTop = container.scrollHeight; 
		})
		.catch(err => {
			alert("Yorum gönderilirken hata oluştu.");
			console.error(err);
		})
		.finally(() => {
			btn.disabled = false;
			btn.innerText = originalText;
		});
}

let currentPage = 1;
let isLoading = false;

function loadMoreActivities() {
	if (isLoading) return;
	isLoading = true;

	const btn = document.getElementById('loadMoreBtn');
	const originalText = btn.innerHTML;
	btn.innerHTML = '<div class="spinner-border spinner-border-sm text-primary"></div> Yükleniyor...';

	currentPage++;

	fetch(`/Home/LoadMoreFeed?page=${currentPage}`)
		.then(res => {
			if (res.status === 204) { // NoContent (Veri bitti)
				btn.style.display = 'none';
				return null;
			}
			return res.text();
		})
		.then(html => {
			if (html) {
				const container = document.getElementById('feedContainer');
				// HTML string'i DOM elementine çevirip ekle
				container.insertAdjacentHTML('beforeend', html);
				btn.innerHTML = originalText;
			}
		})
		.catch(err => {
			console.error(err);
			btn.innerHTML = 'Hata oluştu, tekrar dene';
		})
		.finally(() => {
			isLoading = false;
		});
}