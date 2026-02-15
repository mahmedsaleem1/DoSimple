import api from './api';

export const userService = {
  getUsers: (params) => api.get('/user', { params }),
  getUserById: (id) => api.get(`/user/${id}`),
  updateUser: (id, data) => api.put(`/user/${id}`, data),
  updateUserRole: (id, role) => api.patch(`/user/${id}/role`, { role }),
  verifyUserEmail: (id) => api.patch(`/user/${id}/verify-email`),
  deleteUser: (id) => api.delete(`/user/${id}`),
  getUserStats: () => api.get('/user/stats'),
};
