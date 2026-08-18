// Push registration client. Exposes window.openLearningPush with:
//   register(token)     - registers the service worker and browser push
//                         subscription, then posts it to /push/subscribe
//   unregister(token)   - removes the stored subscription and unregisters SW
//   isSupported()       - whether the browser supports push
(function () {
    'use strict';

    const subscribeEndpoint = '/push/subscribe';

    function isSupported() {
        return 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;
    }

    function getVapidPublicKey() {
        return fetch('/push/vapid-public-key', { credentials: 'same-origin' })
            .then(function (response) { return response.json(); })
            .then(function (data) {
                if (!data.enabled) {
                    return null;
                }
                return data.publicKey;
            })
            .catch(function () { return null; });
    }

    function postSubscription(subscription, token) {
        return subscription.getKey('p256dh').then(function (p256dh) {
            return subscription.getKey('auth').then(function (auth) {
                const body = {
                    endpoint: subscription.endpoint,
                    keys: {
                        p256dh: btoa(String.fromCharCode.apply(null, new Uint8Array(p256dh))),
                        auth: btoa(String.fromCharCode.apply(null, new Uint8Array(auth))),
                    },
                };
                return fetch(subscribeEndpoint, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token,
                    },
                    credentials: 'same-origin',
                    body: JSON.stringify(body),
                }).then(function (response) { return response.ok; });
            });
        });
    }

    function register(token) {
        if (!isSupported()) {
            return Promise.resolve({ ok: false, error: 'Push is not supported by this browser.' });
        }
        return getVapidPublicKey().then(function (publicKey) {
            if (!publicKey) {
                return { ok: false, error: 'Web push is not enabled on the server.' };
            }
            return navigator.serviceWorker.register('/service-worker.js')
                .then(function (registration) {
                    return registration.pushManager.subscribe({
                        userVisibleOnly: true,
                        applicationServerKey: urlBase64ToUint8Array(publicKey),
                    });
                })
                .then(function (subscription) {
                    return postSubscription(subscription, token).then(function (ok) {
                        return ok
                            ? { ok: true }
                            : { ok: false, error: 'The server rejected the subscription.' };
                    });
                });
        });
    }

    function urlBase64ToUint8Array(base64String) {
        const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
        const base64 = (base64String + padding)
            .replace(/-/g, '+')
            .replace(/_/g, '/');
        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);
        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    }

    window.openLearningPush = {
        isSupported: isSupported,
        register: register,
    };
})();
