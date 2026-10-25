const CACHE_NAME = "work-wise-static-v1";
const STATIC_ASSETS = ["/app.webmanifest", "/work-wise-icon.svg", "/favicon.png"];

self.addEventListener("install", event => {
  event.waitUntil(caches.open(CACHE_NAME).then(cache => cache.addAll(STATIC_ASSETS)));
  self.skipWaiting();
});

self.addEventListener("activate", event => {
  event.waitUntil(
    caches.keys().then(keys => Promise.all(
      keys.filter(key => key.startsWith("work-wise-static-") && key !== CACHE_NAME)
          .map(key => caches.delete(key))
    ))
  );
  self.clients.claim();
});

// Cache only public static assets. Never cache authenticated pages, API responses,
// contract documents, salary data, or any other private employment information.
self.addEventListener("fetch", event => {
  const request = event.request;
  const url = new URL(request.url);
  if (request.method !== "GET" || url.origin !== self.location.origin) return;
  if (!STATIC_ASSETS.includes(url.pathname)) return;
  event.respondWith(
    caches.match(request).then(cached => cached || fetch(request))
  );
});