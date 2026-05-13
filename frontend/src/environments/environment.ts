export const environment = {
  production: true,
  apiUrl: '/api',
  hubUrl: '/hubs',
  oidc: {
    issuer: 'https://points.snotaro.dev/authentik/application/o/points-tracker/',
    clientId: 'SDhkCEcb6JbCviinY1itp2Caxsk8lEqz7pbHFiAa',
    scope: 'openid profile email offline_access',
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
    responseType: 'code',
    useSilentRefresh: true,
  },
};
