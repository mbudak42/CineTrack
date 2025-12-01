// Puanlama Modalını Aç
function openRatingModal() {
	var modal = new bootstrap.Modal(document.getElementById('ratingModal'));

	// Razor yerine config'den okuma yapıyoruz
	var currentRating = contentConfig.currentUserRating;
	if (currentRating > 0) {
		var radio = document.getElementById('star' + currentRating);
		if (radio) radio.checked = true;
	}

	modal.show();
}

// Puanı Gönder ve Listeye Ekle
function submitRatingWithList() {
	var ratingInput = document.querySelector('input[name="rating"]:checked');
	if (!ratingInput) {
		alert("Lütfen en az 1 yıldız seçin.");
		return;
	}

	var ratingValue = ratingInput.value;
	// Config'den ID alma
	var contentId = contentConfig.id;
	// İçerik türüne göre varsayılan listeyi belirleme
	var listName = (contentConfig.contentType === "movie" ? "izlediklerim" : "okuduklarim");

	// 1. Puanı API'ye Gönder
	var formData = new FormData();
	formData.append('contentId', contentId);
	formData.append('rating', ratingValue);

	fetch(contentConfig.urls.rateContent, {
		method: 'POST',
		body: formData
	})
		.then(res => res.json())
		.then(data => {
			if (data.success) {
				// 2. Başarılıysa Listeye Ekle
				var listData = new FormData();
				listData.append('contentId', contentId);
				listData.append('listName', listName);

				fetch(contentConfig.urls.addToList, { method: 'POST', body: listData })
					.then(() => {
						// Her şey bitti, sayfayı yenile ki yeni puan görünsün
						location.reload();
					});

			} else {
				alert(data.message);
			}
		})
		.catch(err => {
			console.error(err);
			alert("Bir hata oluştu.");
		});
}

// --- LİSTE VE YORUM İŞLEMLERİ ---

function openCustomListModal() {
	var modal = new bootstrap.Modal(document.getElementById('addCustomListModal'));
	modal.show();

	document.getElementById('listsContainer').innerHTML = '';
	document.getElementById('loadingLists').style.display = 'block';

	fetch(contentConfig.urls.userLists)
		.then(res => res.json())
		.then(lists => {
			document.getElementById('loadingLists').style.display = 'none';
			var container = document.getElementById('listsContainer');

			if (lists.length === 0) {
				container.innerHTML = '<div class="alert alert-warning">Henüz özel bir listeniz yok. Profilinizden oluşturabilirsiniz.</div>';
				return;
			}

			var customLists = lists.filter(l =>
				l.name !== 'izlediklerim' &&
				l.name !== 'izlenecekler' &&
				l.name !== 'okuduklarim' &&
				l.name !== 'okunacaklar'
			);

			if (customLists.length === 0) {
				container.innerHTML = '<div class="alert alert-info">Sadece varsayılan listeleriniz var. Yeni bir özel liste oluşturun.</div>';
				return;
			}

			customLists.forEach(list => {
				var btn = document.createElement('button');
				btn.className = 'list-group-item list-group-item-action d-flex justify-content-between align-items-center';
				btn.innerHTML = `<span>${list.name}</span> <span class="badge bg-secondary rounded-pill">${list.contents ? list.contents.length : 0}</span>`;
				btn.onclick = function () { addToCustomList(list.id); };
				container.appendChild(btn);
			});
		});
}

function addToCustomList(listId) {
	var contentId = contentConfig.id;
	var formData = new FormData();
	formData.append('listId', listId);
	formData.append('contentId', contentId);

	fetch(contentConfig.urls.userListAdd, {
		method: 'POST',
		body: formData
	})
		.then(res => res.json())
		.then(data => {
			alert(data.message);
			var modalEl = document.getElementById('addCustomListModal');
			var modal = bootstrap.Modal.getInstance(modalEl);
			modal.hide();
		});
}

function addToList(contentId, listName) {
	// Bu fonksiyon genellikle parametre ile çağrıldığı için contentId parametresini kullanıyoruz
	const formData = new FormData();
	formData.append('contentId', contentId);
	formData.append('listName', listName);

	fetch(contentConfig.urls.addToList, {
		method: 'POST',
		body: formData
	})
		.then(response => response.json())
		.then(data => {
			alert(data.message);
		})
		.catch(error => {
			console.error('Error:', error);
			alert("Bir hata oluştu.");
		});
}

function submitComment(id) {
	var commentText = document.getElementById("newComment").value;
	if (!commentText) {
		alert("Lütfen bir yorum yazın.");
		return;
	}

	var formData = new FormData();
	formData.append('contentId', id || contentConfig.id);
	formData.append('text', commentText);

	fetch(contentConfig.urls.addComment, {
		method: 'POST',
		body: formData
	})
		.then(res => res.json())
		.then(data => {
			if (data.success) {
				location.reload();
			} else {
				alert(data.message);
			}
		})
		.catch(err => {
			console.error(err);
			alert("Bir hata oluştu.");
		});
}

// Modal'ı aç
function openReviewModal() {
	var modal = new bootstrap.Modal(document.getElementById('reviewModal'));
	modal.show();
}

// Yorum Gönder / Güncelle (Modal İçinden)
function submitReviewFromModal() {
	var text = document.getElementById("modalReviewText").value;
	var contentId = contentConfig.id;

	if (!text.trim()) {
		alert("Lütfen bir şeyler yazın.");
		return;
	}

	var formData = new FormData();
	formData.append('contentId', contentId);
	formData.append('text', text);

	fetch(contentConfig.urls.addComment, {
		method: 'POST',
		body: formData
	})
		.then(res => res.json())
		.then(data => {
			if (data.success) {
				location.reload();
			} else {
				alert(data.message);
			}
		})
		.catch(err => console.error(err));
}

// Yorum Sil
function deleteReview() {
	if (!confirm("Yorumunuzu silmek istediğinize emin misiniz?")) return;

	var formData = new FormData();
	formData.append('contentId', contentConfig.id);

	fetch(contentConfig.urls.deleteReview, {
		method: 'POST',
		body: formData
	})
		.then(res => res.json())
		.then(data => {
			if (data.success) {
				location.reload();
			} else {
				alert(data.message);
			}
		});
}