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

export default api;