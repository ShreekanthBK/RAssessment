import React, { useState, useEffect } from 'react';
import './App.css';

const API_BASE = 'https://localhost:7042/api';

// Error Boundary Component
class ErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, error: null, errorInfo: null };
  }

  static getDerivedStateFromError(error) {
    return { hasError: true };
  }

  componentDidCatch(error, errorInfo) {
    this.setState({
      error: error,
      errorInfo: errorInfo
    });
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="error-boundary">
          <h2>🚨 Something went wrong!</h2>
          <p>The application encountered an unexpected error. Please refresh the page to try again.</p>
          <button onClick={() => window.location.reload()} className="refresh-btn">
            🔄 Refresh Page
          </button>
          {process.env.NODE_ENV === 'development' && (
            <details className="error-details">
              <summary>Error Details (Development)</summary>
              <pre>{this.state.error && this.state.error.toString()}</pre>
              <pre>{this.state.errorInfo.componentStack}</pre>
            </details>
          )}
        </div>
      );
    }

    return this.props.children;
  }
}

function App() {
  const [board, setBoard] = useState({ columns: [] });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showTaskForm, setShowTaskForm] = useState(false);
  const [showTaskDetail, setShowTaskDetail] = useState(false);
  const [selectedTask, setSelectedTask] = useState(null);
  const [newTask, setNewTask] = useState({
    name: '',
    description: '',
    deadline: '',
    columnId: 1,
    files: []
  });

  // Clear error after 5 seconds
  useEffect(() => {
    if (error) {
      const timer = setTimeout(() => setError(null), 5000);
      return () => clearTimeout(timer);
    }
  }, [error]);

  // Validate image files
  const validateImageFiles = (files) => {
    const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp'];
    const maxSize = 5 * 1024 * 1024; // 5MB
    const validFiles = [];
    const errors = [];

    for (const file of files) {
      if (!validTypes.includes(file.type)) {
        errors.push(`${file.name}: Only image files (JPEG, PNG, GIF, WebP) are allowed`);
        continue;
      }
      if (file.size > maxSize) {
        errors.push(`${file.name}: File size must be less than 5MB`);
        continue;
      }
      validFiles.push(file);
    }

    if (errors.length > 0) {
      setError(errors.join('. '));
    }

    return validFiles;
  };

  // Safe API call wrapper
  const safeApiCall = async (apiCall, errorMessage = 'An error occurred') => {
    try {
      return await apiCall();
    } catch (err) {
      console.error(errorMessage, err);
      setError(`${errorMessage}: ${err.message || 'Please try again'}`);
      throw err;
    }
  };

  useEffect(() => {
    fetchBoard();
  }, []);

  const fetchBoard = async () => {
    await safeApiCall(async () => {
      const response = await fetch(`${API_BASE}/board`);
      if (!response.ok) throw new Error(`HTTP ${response.status}: Failed to fetch board data`);
      const data = await response.json();
      setBoard(data);
      setLoading(false);
    }, 'Failed to load board');
  };

  const createTask = async (e) => {
    e.preventDefault();
    
    if (!newTask.name.trim()) {
      setError('Task name is required');
      return;
    }

    await safeApiCall(async () => {
      const response = await fetch(`${API_BASE}/tasks`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          name: newTask.name.trim(),
          description: newTask.description.trim(),
          deadline: newTask.deadline || null,
          columnId: newTask.columnId
        }),
      });
      if (!response.ok) throw new Error(`Failed to create task (HTTP ${response.status})`);
      
      const createdTask = await response.json();
      
      // Upload attachments if any files were selected
      if (newTask.files.length > 0) {
        const validFiles = validateImageFiles(newTask.files);
        let uploadedCount = 0;
        
        for (const file of validFiles) {
          try {
            await uploadAttachment(createdTask.id, file);
            uploadedCount++;
          } catch (uploadError) {
            console.error('Failed to upload attachment:', uploadError);
            setError(`Failed to upload ${file.name}. Task created successfully.`);
          }
        }
        
        if (uploadedCount > 0) {
          console.log(`Successfully uploaded ${uploadedCount} image(s)`);
        }
      }
      
      setNewTask({ name: '', description: '', deadline: '', columnId: 1, files: [] });
      setShowTaskForm(false);
      fetchBoard();
    }, 'Failed to create task');
  };

    const toggleFavorite = async (taskId, currentFavorite) => {
    try {
      // Find the task to get current values
      const task = board.columns.flatMap(col => col.tasks).find(t => t.id === taskId);
      if (!task) throw new Error('Task not found');

      const response = await fetch(`${API_BASE}/tasks/${taskId}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          name: task.name,
          description: task.description,
          deadline: task.deadline,
          isFavorite: !currentFavorite,
          columnId: task.columnId
        }),
      });
      if (!response.ok) throw new Error('Failed to update favorite');
      fetchBoard();
    } catch (err) {
      setError(err.message);
    }
  };

  const uploadAttachment = async (taskId, file) => {
    return await safeApiCall(async () => {
      // Validate image file
      const validFiles = validateImageFiles([file]);
      if (validFiles.length === 0) {
        throw new Error('Invalid image file');
      }

      const formData = new FormData();
      formData.append('file', file);

      const response = await fetch(`${API_BASE}/attachments/tasks/${taskId}`, {
        method: 'POST',
        body: formData,
      });
      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Upload failed (HTTP ${response.status}): ${errorText}`);
      }
      
      fetchBoard(); // Refresh to show new attachment count
      return await response.json();
    }, `Failed to upload ${file.name}`);
  };

  const deleteAttachment = async (attachmentId) => {
    try {
      const response = await fetch(`${API_BASE}/attachments/${attachmentId}`, {
        method: 'DELETE',
      });
      if (!response.ok) throw new Error('Failed to delete attachment');
      fetchBoard(); // Refresh to update attachment count
    } catch (err) {
      setError(err.message);
    }
  };

  const downloadAttachment = async (attachmentId, fileName) => {
    try {
      const response = await fetch(`${API_BASE}/attachments/${attachmentId}/download`);
      if (!response.ok) throw new Error('Failed to download attachment');
      
      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = fileName;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err) {
      setError(err.message);
    }
  };

  const openTaskDetail = (task) => {
    setSelectedTask(task);
    setShowTaskDetail(true);
  };

  const closeTaskDetail = () => {
    setShowTaskDetail(false);
    setSelectedTask(null);
  };

  const deleteTask = async (taskId) => {
    if (!window.confirm('Are you sure you want to delete this task?')) return;
    
    try {
      const response = await fetch(`${API_BASE}/tasks/${taskId}`, {
        method: 'DELETE',
      });
      if (!response.ok) throw new Error('Failed to delete task');
      fetchBoard();
    } catch (err) {
      setError(err.message);
    }
  };

  const moveTask = async (taskId, newColumnId) => {
    try {
      const response = await fetch(`${API_BASE}/tasks/${taskId}/move`, {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          columnId: newColumnId,
          sortOrder: 1
        }),
      });
      if (!response.ok) throw new Error('Failed to move task');
      fetchBoard();
    } catch (err) {
      setError(err.message);
    }
  };

  // Drag and Drop handlers
  const [draggedTask, setDraggedTask] = useState(null);
  const [dragOverColumn, setDragOverColumn] = useState(null);

  const handleDragStart = (e, task) => {
    setDraggedTask(task);
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/html', e.target.outerHTML);
    e.target.classList.add('dragging');
  };

  const handleDragEnd = (e) => {
    e.target.classList.remove('dragging');
    setDraggedTask(null);
    setDragOverColumn(null);
  };

  const handleDragOver = (e) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
  };

  const handleDragEnter = (e, columnId) => {
    e.preventDefault();
    setDragOverColumn(columnId);
  };

  const handleDragLeave = (e) => {
    e.preventDefault();
    // Only clear if we're leaving the column container, not a child element
    if (!e.currentTarget.contains(e.relatedTarget)) {
      setDragOverColumn(null);
    }
  };

  const handleDrop = (e, columnId) => {
    e.preventDefault();
    setDragOverColumn(null);
    
    if (draggedTask && draggedTask.columnId !== columnId) {
      moveTask(draggedTask.id, columnId);
    }
    setDraggedTask(null);
  };

  if (loading) return <div className="loading">⏳ Loading task board...</div>;

  return (
    <div className="App">
      {/* Global Error Notification */}
      {error && (
        <div className="error-notification">
          <span>❌ {error}</span>
          <button onClick={() => setError(null)} className="close-error">✕</button>
        </div>
      )}
      
      <header className="app-header">
        <h1>🎯 Task Management Board</h1>
        <button 
          className="add-task-btn"
          onClick={() => setShowTaskForm(true)}
        >
          + Add Task
        </button>
      </header>

      {showTaskForm && (
        <div className="modal-overlay">
          <div className="modal">
            <h2>Create New Task</h2>
            <form onSubmit={createTask}>
              <input
                type="text"
                placeholder="Task name"
                value={newTask.name}
                onChange={(e) => setNewTask({...newTask, name: e.target.value})}
                required
              />
              <textarea
                placeholder="Description"
                value={newTask.description}
                onChange={(e) => setNewTask({...newTask, description: e.target.value})}
              />
              <input
                type="date"
                value={newTask.deadline}
                onChange={(e) => setNewTask({...newTask, deadline: e.target.value})}
              />
              <select
                value={newTask.columnId}
                onChange={(e) => setNewTask({...newTask, columnId: parseInt(e.target.value)})}
              >
                {board.columns.map(column => (
                  <option key={column.id} value={column.id}>
                    {column.name}
                  </option>
                ))}
              </select>
              
              {/* File Upload Section */}
              <div className="file-upload-section">
                <label>Image Attachments (optional)</label>
                <input
                  type="file"
                  multiple
                  accept="image/*"
                  onChange={(e) => {
                    const selectedFiles = Array.from(e.target.files);
                    const validFiles = validateImageFiles(selectedFiles);
                    setNewTask({...newTask, files: validFiles});
                  }}
                  className="file-input"
                />
                <p className="file-hint">Only image files (JPEG, PNG, GIF, WebP) up to 5MB each</p>
                {newTask.files.length > 0 && (
                  <div className="selected-files">
                    <p>Selected images:</p>
                    <ul>
                      {newTask.files.map((file, index) => (
                        <li key={index}>
                          �️ {file.name} ({Math.round(file.size / 1024)} KB)
                          <button
                            type="button"
                            onClick={() => {
                              const newFiles = newTask.files.filter((_, i) => i !== index);
                              setNewTask({...newTask, files: newFiles});
                            }}
                            className="remove-file-btn"
                          >
                            ✕
                          </button>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
              
              <div className="form-actions">
                <button type="submit">Create Task</button>
                <button type="button" onClick={() => setShowTaskForm(false)}>
                  Cancel
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      <div className="board">
        {board.columns.map(column => (
          <div 
            key={column.id} 
            className={`column ${dragOverColumn === column.id ? 'drag-over' : ''}`}
            onDragOver={handleDragOver}
            onDragEnter={(e) => handleDragEnter(e, column.id)}
            onDragLeave={handleDragLeave}
            onDrop={(e) => handleDrop(e, column.id)}
          >
            <h2 className="column-header">
              {column.name} 
              <span className="task-count">({column.tasks.length})</span>
            </h2>
            <div className="tasks">
              {column.tasks.map(task => (
                <div 
                  key={task.id} 
                  className={`task ${task.isFavorite ? 'favorite' : ''}`}
                  draggable="true"
                  onDragStart={(e) => handleDragStart(e, task)}
                  onDragEnd={handleDragEnd}
                >
                  <div className="task-header">
                    <h3>{task.name}</h3>
                    <div className="task-actions">
                      <button
                        className={`favorite-btn ${task.isFavorite ? 'active' : ''}`}
                        onClick={() => toggleFavorite(task.id, task.isFavorite)}
                        title={task.isFavorite ? 'Remove from favorites' : 'Add to favorites'}
                      >
                        ⭐
                      </button>
                      <select
                        className="move-select"
                        value={task.columnId}
                        onChange={(e) => moveTask(task.id, parseInt(e.target.value))}
                        title="Move to column"
                      >
                        {board.columns.map(col => (
                          <option key={col.id} value={col.id}>
                            {col.name}
                          </option>
                        ))}
                      </select>
                      <button
                        className="delete-btn"
                        onClick={() => deleteTask(task.id)}
                        title="Delete task"
                      >
                        🗑️
                      </button>
                    </div>
                  </div>
                  {task.description && (
                    <p className="task-description">{task.description}</p>
                  )}
                  {task.deadline && (
                    <div className="task-deadline">
                      📅 Due: {new Date(task.deadline).toLocaleDateString()}
                    </div>
                  )}
                  {task.attachments.length > 0 && (
                    <div 
                      className="task-attachments clickable"
                      onClick={() => openTaskDetail(task)}
                      title="View attachments"
                    >
                      📎 {task.attachments.length} attachment(s)
                    </div>
                  )}
                  <div className="task-actions">
                    <button
                      className="detail-btn"
                      onClick={() => openTaskDetail(task)}
                      title="View details & attachments"
                    >
                      👁️ Details
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>

      <footer className="app-footer">
        <p>✨ Features: Task Management • Favorites • Sorting • File Attachments</p>
        <p>🔧 Built with .NET 9 Web API & React • Fully tested with NUnit</p>
      </footer>

      {/* Task Detail Modal */}
      {showTaskDetail && selectedTask && (
        <TaskDetailModal 
          task={selectedTask}
          onClose={closeTaskDetail}
          onUploadAttachment={uploadAttachment}
          onDeleteAttachment={deleteAttachment}
          onDownloadAttachment={downloadAttachment}
          onUpdateTask={fetchBoard}
        />
      )}
    </div>
  );
}

// Task Detail Modal Component  
function TaskDetailModal({ task, onClose, onUploadAttachment, onDeleteAttachment, onDownloadAttachment, onUpdateTask }) {
  const [uploading, setUploading] = useState(false);
  const [dragOver, setDragOver] = useState(false);
  const [modalError, setModalError] = useState(null);

  // Clear modal error after 3 seconds
  useEffect(() => {
    if (modalError) {
      const timer = setTimeout(() => setModalError(null), 3000);
      return () => clearTimeout(timer);
    }
  }, [modalError]);

  // Validate image files for modal
  const validateModalImageFiles = (files) => {
    const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp'];
    const maxSize = 5 * 1024 * 1024; // 5MB
    const validFiles = [];
    const errors = [];

    for (const file of files) {
      if (!validTypes.includes(file.type)) {
        errors.push(`${file.name}: Only image files are allowed`);
        continue;
      }
      if (file.size > maxSize) {
        errors.push(`${file.name}: File size must be less than 5MB`);
        continue;
      }
      validFiles.push(file);
    }

    if (errors.length > 0) {
      setModalError(errors.join('. '));
    }

    return validFiles;
  };

  const handleFileUpload = async (files) => {
    if (!files || files.length === 0) return;
    
    const validFiles = validateModalImageFiles(files);
    if (validFiles.length === 0) return;
    
    setUploading(true);
    setModalError(null);
    
    try {
      let successCount = 0;
      for (const file of validFiles) {
        try {
          await onUploadAttachment(task.id, file);
          successCount++;
        } catch (err) {
          console.error('Upload failed:', err);
          setModalError(`Failed to upload ${file.name}: ${err.message}`);
        }
      }
      
      if (successCount > 0) {
        onUpdateTask(); // Refresh the task data
      }
    } finally {
      setUploading(false);
    }
  };

  const handleDrop = (e) => {
    e.preventDefault();
    setDragOver(false);
    const files = Array.from(e.dataTransfer.files);
    handleFileUpload(files);
  };

  const handleDragOver = (e) => {
    e.preventDefault();
    setDragOver(true);
  };

  const handleDragLeave = (e) => {
    e.preventDefault();
    setDragOver(false);
  };

  const handleFileSelect = (e) => {
    const files = Array.from(e.target.files);
    handleFileUpload(files);
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>{task.name}</h2>
          <button className="close-btn" onClick={onClose}>✕</button>
        </div>
        
        <div className="modal-body">
          <div className="task-info">
            <p><strong>Description:</strong> {task.description || 'No description'}</p>
            {task.deadline && (
              <p><strong>Deadline:</strong> {new Date(task.deadline).toLocaleDateString()}</p>
            )}
            <p><strong>Status:</strong> {task.isFavorite ? '⭐ Favorite' : 'Normal'}</p>
          </div>

          <div className="attachments-section">
            <h3>Image Attachments ({task.attachments.length})</h3>
            
            {/* Error display for modal */}
            {modalError && (
              <div className="modal-error">
                ⚠️ {modalError}
              </div>
            )}
            
            {/* File Upload Area */}
            <div 
              className={`upload-area ${dragOver ? 'drag-over' : ''}`}
              onDrop={handleDrop}
              onDragOver={handleDragOver}
              onDragLeave={handleDragLeave}
              onClick={() => document.getElementById('file-input').click()}
            >
              {uploading ? (
                <p>📤 Uploading images...</p>
              ) : (
                <>
                  <p>�️ Drop images here or click to upload</p>
                  <p className="upload-hint">Supports JPEG, PNG, GIF, WebP (max 5MB each)</p>
                </>
              )}
              <input
                id="file-input"
                type="file"
                multiple
                accept="image/*"
                onChange={handleFileSelect}
                style={{ display: 'none' }}
              />
            </div>

            {/* Attachment List */}
            <div className="attachment-list">
              {task.attachments.map(attachment => (
                <div key={attachment.id} className="attachment-item">
                  <div className="attachment-info">
                    <span className="attachment-name">🖼️ {attachment.fileName}</span>
                    <span className="attachment-size">
                      ({Math.round(attachment.fileSize / 1024)} KB)
                    </span>
                  </div>
                  <div className="attachment-actions">
                    <button 
                      className="download-btn"
                      onClick={() => {
                        try {
                          onDownloadAttachment(attachment.id, attachment.fileName);
                        } catch (err) {
                          setModalError(`Failed to download ${attachment.fileName}`);
                        }
                      }}
                      title="Download image"
                    >
                      📥
                    </button>
                    <button 
                      className="delete-attachment-btn"
                      onClick={() => {
                        if (window.confirm(`Delete ${attachment.fileName}?`)) {
                          try {
                            onDeleteAttachment(attachment.id);
                          } catch (err) {
                            setModalError(`Failed to delete ${attachment.fileName}`);
                          }
                        }
                      }}
                      title="Delete image"
                    >
                      🗑️
                    </button>
                  </div>
                </div>
              ))}
              {task.attachments.length === 0 && (
                <p className="no-attachments">No images attached yet</p>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

// Main App Wrapper with Error Boundary
function AppWrapper() {
  return (
    <ErrorBoundary>
      <App />
    </ErrorBoundary>
  );
}

export default AppWrapper;
