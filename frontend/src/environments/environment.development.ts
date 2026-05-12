export const environment = {
  production: false,
  apiUrl: 'http://localhost:8080/api',
  hubUrl: 'http://localhost:8080/hubs',
  oidc: {
    issuer: 'https://auth.yourdomain.com/application/o/points-tracker/',
    clientId: 'points-tracker',
    scope: 'openid profile email offline_access',
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
    responseType: 'code',
    useSilentRefresh: false,
  },
};
