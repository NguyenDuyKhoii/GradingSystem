import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5257/api',
});

api.interceptors.request.use(
  (config) => {
    let token = localStorage.getItem('token');

    if (token) {
      token = token.trim();

      // Nếu token bị lưu kèm dấu "..."
      token = token.replace(/^"|"$/g, '');

      // Nếu token đã có chữ Bearer thì không thêm lần nữa
      config.headers.Authorization = token.startsWith('Bearer ')
        ? token
        : `Bearer ${token}`;
    }

    return config;
  },
  (error) => Promise.reject(error)
);

export default api;