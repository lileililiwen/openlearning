// Service worker for web push. Shows the payload as a notification and
// opens the linked page on click.
self.addEventListener('push', function (event) {
    let data = { title: 'OpenLearning', body: '', link: '/' };
    if (event.data) {
        try {
            data = Object.assign(data, event.data.json());
        } catch (_) {
            data.body = event.data.text();
        }
    }

    event.waitUntil(
        self.registration.showNotification(data.title, {
            body: data.body,
            icon: '/favicon.ico',
            badge: '/favicon.ico',
            data: { link: data.link },
        })
    );
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    const link = (event.notification.data && event.notification.data.link) || '/';
    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (clientList) {
            for (const client of clientList) {
                if ('focus' in client) {
                    client.navigate(link);
                    return client.focus();
                }
            }
            return clients.openWindow(link);
        })
    );
});
