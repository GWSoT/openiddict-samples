/* global window, fetch, Oidc */

let origin = `${window.location.protocol}//${window.location.hostname}`;
if (window.location.port) {
  origin += `:${window.location.port}`;
}

//
// Define helper functions.
//
const fetchUserResources = (accessToken) => {
  const url = `${origin}/api`;
  const options = {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  };

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
  authority: origin,
  silent_redirect_uri: origin,
  client_id: 'my-client',
  response_type: 'id_token token',
  scope: 'openid',
  loadUserInfo: false,
  silentRequestTimeout: 10000,
};

const userManager = new Oidc.UserManager(settings);

//
// Run the demo.
//
if (window !== window.parent) {
  // We are in an iframe, so only process the silent sign in callback.
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
