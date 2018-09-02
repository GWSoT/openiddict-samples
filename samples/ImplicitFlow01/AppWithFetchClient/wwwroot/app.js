/* global window, fetch, Oidc */

//
// Define helper functions.
//
const fetchUserResources = (accessToken) => {
  const url = `${window.location.origin}/api`;
  const options = {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  };

  // See https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API
  fetch(url, options)
    .then(response => response.json())
    .then(obj => JSON.stringify(obj))
    .then((json) => {
      window.document.getElementById('fetch_response').innerText = json;
    })
    .catch(err => console.log(err)); // eslint-disable-line no-console
};

const displayUserData = (user) => {
  const json = JSON.stringify(user, null, 4);
  window.document.getElementById('signin_response').innerText = json;
};

const signinSilentError = (err) => {
  window.document.getElementById('signin_response').innerText = err;
};

//
// Configure the oidc-client.js UserManager.
//
const settings = {
  authority: window.location.origin,
  silent_redirect_uri: window.location.origin,
  client_id: 'my-client',
  response_type: 'id_token token',
  scope: 'openid',
  loadUserInfo: false,
  silentRequestTimeout: 10000,
};

const userManager = new Oidc.UserManager(settings);

//
// Check whether this JavaScript is loading in an iframe.
//
const isIframe = window !== window.parent;

//
// Run the demo.
//
if (isIframe) {
  // If we are in an iframe, only process the silent sign in callback.
  // This prevents an infinite loop.
  // Silent sign in always happens in child iframe.
  userManager.signinSilentCallback().then(() => {
    // Optional: communicate from the child iframe to its parent.
    // The userManager already does that, and we can optionally do more.
    window.parent.postMessage('signinSilentCallback -> complete', '*');
  });
} else {
  // Optional: receive message from the child iframe.
  window.addEventListener('message', (event) => {
    console.log(`message from iframe: ${event.data}`); // eslint-disable-line no-console
  });

  userManager.signinSilent()
    .then((user) => {
      displayUserData(user);
      fetchUserResources(user.access_token);
    })
    .catch(signinSilentError);
}
