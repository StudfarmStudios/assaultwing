/**
 * Serves the current Assault Wing 2 browser publish from R2.
 */

const REVALIDATE = 'public, max-age=60';

function securityHeaders(headers, isDocument) {
  // These are harmless for today's single-threaded build and keep the host
  // ready for a future pthread-enabled .NET WebAssembly runtime.
  headers.set('Cross-Origin-Opener-Policy', 'same-origin');
  headers.set('Cross-Origin-Embedder-Policy', 'require-corp');
  headers.set('Cross-Origin-Resource-Policy', isDocument ? 'cross-origin' : 'same-origin');
  headers.set('X-Content-Type-Options', 'nosniff');
}

function errorResponse(message, status) {
  const headers = new Headers({
    'Content-Type': 'text/plain; charset=utf-8',
    'Cache-Control': 'no-store',
  });
  securityHeaders(headers, true);
  return new Response(message, { status, headers });
}

export default {
  async fetch(request, env) {
    if (request.method !== 'GET' && request.method !== 'HEAD') {
      const response = errorResponse('Method Not Allowed', 405);
      response.headers.set('Allow', 'GET, HEAD');
      return response;
    }

    const url = new URL(request.url);
    let key;
    try {
      key = url.pathname === '/' ? 'index.html' : decodeURIComponent(url.pathname.slice(1));
    } catch {
      return errorResponse('Bad Request', 400);
    }
    if (key.endsWith('/')) key += 'index.html';

    const object = await env.ASSETS.get(key, { onlyIf: request.headers });
    if (object === null) return errorResponse('Not Found', 404);

    const headers = new Headers();
    object.writeHttpMetadata(headers);
    headers.set('ETag', object.httpEtag);
    headers.set('Cache-Control', REVALIDATE);
    securityHeaders(headers, key.endsWith('.html'));

    if (!('body' in object)) {
      return new Response(null, { status: 304, headers });
    }

    return new Response(request.method === 'HEAD' ? null : object.body, {
      headers,
      // R2 contains the already-Brotli-encoded wire bytes for compressible
      // files. Preserve that single encoding layer exactly as uploaded.
      encodeBody: 'manual',
    });
  },
};
