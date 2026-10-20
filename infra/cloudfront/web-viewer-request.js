import cf from 'cloudfront';

const MAIN_HOST = 'noisevault.net';
const ENSAIO_HOST = `ensaio.${MAIN_HOST}`;
const API_ORIGIN_ID = 'noisevault-api';
const PREVIEW_BOTS = /facebookexternalhit|facebot|whatsapp|twitterbot|telegrambot|discordbot|slackbot|linkedinbot|skypeuripreview/i;
const SHAREABLE_PAGE = /^\/(feed|mural|eventos|bandas|perfil)\/[^\/]+$/;
const STATIC_FILE = /\.(css|js|mjs|map|png|jpg|jpeg|gif|svg|ico|webp|avif|woff|woff2|ttf|json|webmanifest|txt|xml|html)$/i;

function header(request, name) {
    const value = request.headers[name];
    return value ? value.value : '';
}

function queryString(request) {
    const parts = [];
    for (const key in request.querystring) {
        const entry = request.querystring[key];
        const values = entry.multiValue ? entry.multiValue : [entry];
        for (let i = 0; i < values.length; i++) {
            parts.push(values[i].value === '' ? key : `${key}=${values[i].value}`);
        }
    }
    return parts.length ? `?${parts.join('&')}` : '';
}

function handler(event) {
    const request = event.request;
    const host = header(request, 'host');
    const uri = request.uri;

    if (host === `www.${MAIN_HOST}`) {
        return {
            statusCode: 301,
            statusDescription: 'Moved Permanently',
            headers: { location: { value: `https://${MAIN_HOST}${uri}${queryString(request)}` } },
        };
    }

    if (uri.startsWith('/s/')) {
        cf.selectRequestOriginById(API_ORIGIN_ID);
        return request;
    }

    const page = uri.length > 1 && uri.endsWith('/') ? uri.slice(0, -1) : uri;
    if (host !== ENSAIO_HOST && SHAREABLE_PAGE.test(page) && PREVIEW_BOTS.test(header(request, 'user-agent'))) {
        request.uri = `/s${page}`;
        request.querystring = {};
        cf.selectRequestOriginById(API_ORIGIN_ID);
        return request;
    }

    if (!STATIC_FILE.test(uri)) {
        request.uri = '/index.html';
    }

    return request;
}
