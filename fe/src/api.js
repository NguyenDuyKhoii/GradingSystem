import axios from 'axios';

// Main Grading API Instance (Port 5000)
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
});

// User & Identity API Instance (Port 5002)
export const userApi = axios.create({
  baseURL: import.meta.env.VITE_USER_API_URL || 'http://localhost:5002/api',
});

const attachToken = (config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
};

api.interceptors.request.use(attachToken, (error) => Promise.reject(error));
userApi.interceptors.request.use(attachToken, (error) => Promise.reject(error));

export default api;
