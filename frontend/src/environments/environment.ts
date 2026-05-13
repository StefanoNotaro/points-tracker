export const environment = {
  production: true,
  apiUrl: '/api',
  hubUrl: '/hubs',
  oidc: {
    issuer: 'https://auth.snotaro.dev/application/o/points-tracker/',
    clientId: 'points-tracker',
    scope: 'openid profile email offline_access',
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
    responseType: 'code',
    useSilentRefresh: true,
  },
};
