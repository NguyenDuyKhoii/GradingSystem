import axios from 'axios';

// Single Entry Point via API Gateway (Port 8000)
const GATEWAY_URL = import.meta.env.VITE_API_URL || 'http://localhost:8000/api';

// Main Grading API Instance (via Gateway)
const api = axios.create({
  baseURL: GATEWAY_URL,
});

// User & Identity API Instance (via Gateway)
export const userApi = axios.create({
  baseURL: GATEWAY_URL,
});

const attachToken = (config) => {
  let token = localStorage.getItem('token');
  if (token) {
    token = token.trim().replace(/^"|"$/g, '');
    config.headers.Authorization = token.startsWith('Bearer ') ? token : `Bearer ${token}`;
  }
  return config;
};

api.interceptors.request.use(attachToken, (error) => Promise.reject(error));
userApi.interceptors.request.use(attachToken, (error) => Promise.reject(error));

// Flag to prevent multiple simultaneous refresh attempts
let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

const handleUnauthorizedResponse = (instance) => {
  instance.interceptors.response.use(
    (response) => response,
    async (error) => {
      const originalRequest = error.config;

      // Avoid infinite loop if refresh call itself fails
      if (
        error.response?.status === 401 &&
        !originalRequest._retry &&
        !originalRequest.url?.includes('/Auth/login') &&
        !originalRequest.url?.includes('/Auth/refresh')
      ) {
        const refreshToken = localStorage.getItem('refreshToken');

        if (!refreshToken) {
          localStorage.removeItem('token');
          localStorage.removeItem('refreshToken');
          localStorage.removeItem('user');
          window.location.reload();
          return Promise.reject(error);
        }

        if (isRefreshing) {
          return new Promise((resolve, reject) => {
            failedQueue.push({ resolve, reject });
          })
            .then((token) => {
              originalRequest.headers.Authorization = `Bearer ${token}`;
              return instance(originalRequest);
            })
            .catch((err) => Promise.reject(err));
        }

        originalRequest._retry = true;
        isRefreshing = true;

        try {
          const res = await axios.post(`${GATEWAY_URL}/Auth/refresh`, {
            refreshToken: refreshToken.trim().replace(/^"|"$/g, ''),
          });

          const { token: newAccessToken, refreshToken: newRefreshToken } = res.data;

          localStorage.setItem('token', newAccessToken);
          if (newRefreshToken) {
            localStorage.setItem('refreshToken', newRefreshToken);
          }

          instance.defaults.headers.common.Authorization = `Bearer ${newAccessToken}`;
          originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;

          processQueue(null, newAccessToken);
          return instance(originalRequest);
        } catch (refreshErr) {
          processQueue(refreshErr, null);
          localStorage.removeItem('token');
          localStorage.removeItem('refreshToken');
          localStorage.removeItem('user');
          window.location.reload();
          return Promise.reject(refreshErr);
        } finally {
          isRefreshing = false;
        }
      }

      return Promise.reject(error);
    }
  );
};

handleUnauthorizedResponse(api);
handleUnauthorizedResponse(userApi);

export default api;