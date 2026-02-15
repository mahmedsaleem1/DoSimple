import api from './api';

export const taskService = {
  // GET
  getTasks: (params) => api.get('/task', { params }),
  getTaskById: (id) => api.get(`/task/${id}`),
  getTaskStats: () => api.get('/task/stats'),
  getCategories: () => api.get('/task/categories'),
  getMyAssigned: (params) => api.get('/task/my-assigned', { params }),
  getMyCreated: (params) => api.get('/task/my-created', { params }),
  getOverdue: (params) => api.get('/task/overdue', { params }),

  // CREATE
  createTask: (data, image) => {
    const formData = new FormData();
    Object.entries(data).forEach(([key, value]) => {
      if (value !== null && value !== undefined && value !== '') {
        formData.append(key, value);
      }
    });
    if (image) formData.append('image', image);
    return api.post('/task', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  // UPDATE
  updateTask: (id, data, image) => {
    const formData = new FormData();
    Object.entries(data).forEach(([key, value]) => {
      if (value !== null && value !== undefined && value !== '') {
        formData.append(key, value);
      }
    });
    if (image) formData.append('image', image);
    return api.put(`/task/${id}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  updateTaskStatus: (id, status) => api.patch(`/task/${id}/status`, { status }),
  assignTask: (id, userId) => api.put(`/task/${id}/assign`, { userId }),
  unassignTask: (id) => api.put(`/task/${id}/unassign`),

  // BULK
  bulkDelete: (taskIds) => api.post('/task/bulk-delete', taskIds),
  bulkUpdateStatus: (taskIds, status) =>
    api.post('/task/bulk-update-status', { taskIds, status }),

  // DELETE
  deleteTask: (id) => api.delete(`/task/${id}`),
};
