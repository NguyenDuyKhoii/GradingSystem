import React, { useState, useEffect } from 'react';
import { BookOpen, Award, Layers, Plus, Trash2, Calendar, ClipboardCheck, Edit3, AlertTriangle } from 'lucide-react';
import api from '../api';

export default function AcademicDashboard() {
  const [activeTab, setActiveTab] = useState('subjects'); // subjects, rubrics, classes

  // Subjects state
  const [subjects, setSubjects] = useState([]);
  const [subCode, setSubCode] = useState('');
  const [subName, setSubName] = useState('');
  const [editingSubject, setEditingSubject] = useState(null); // null = create mode

  // Rubrics state
  const [selectedSubId, setSelectedSubId] = useState('');
  const [rubricName, setRubricName] = useState('');
  const [currentRubric, setCurrentRubric] = useState(null);
  const [editingRubricId, setEditingRubricId] = useState(null);
  const [criteria, setCriteria] = useState([
    { criteriaName: 'Content', description: 'Quality of writing and relevance', maxPoints: 10, weight: 50 },
    { criteriaName: 'Grammar', description: 'Grammar and vocabulary correctness', maxPoints: 10, weight: 50 }
  ]);



  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [confirmModal, setConfirmModal] = useState({ show: false, title: '', message: '', onConfirm: null });

  const showConfirm = (title, msg, onConfirm) => {
    setConfirmModal({ show: true, title, message: msg, onConfirm });
  };

  const closeConfirm = () => {
    setConfirmModal({ show: false, title: '', message: '', onConfirm: null });
  };

  useEffect(() => {
    fetchSubjects();
  }, []);

  const fetchSubjects = async () => {
    try {
      const res = await api.get('/Subjects');
      setSubjects(res.data);
      if (res.data.length > 0 && !selectedSubId) {
        setSelectedSubId(res.data[0].id.toString());
        fetchRubric(res.data[0].id);
      }
    } catch (err) {
      console.error(err);
    }
  };



  const fetchRubric = async (subId) => {
    try {
      setCurrentRubric(null);
      const res = await api.get(`/Rubrics/subject/${subId}`);
      setCurrentRubric(res.data);
    } catch (err) {
      // 404 is normal if no rubric is defined yet
      if (err.response?.status !== 404) {
        console.error(err);
      }
    }
  };

  const handleSubjectChange = (e) => {
    const subId = e.target.value;
    setSelectedSubId(subId);
    setEditingRubricId(null);
    fetchRubric(subId);
  };

  // Add/Update Subject
  const handleAddSubject = async (e) => {
    e.preventDefault();
    setError('');
    setMessage('');
    setLoading(true);

    try {
      if (editingSubject) {
        await api.put(`/Subjects/${editingSubject.id}`, {
          id: editingSubject.id,
          subjectCode: subCode,
          subjectName: subName
        });
        setMessage('Subject updated successfully!');
        setEditingSubject(null);
      } else {
        await api.post('/Subjects', { subjectCode: subCode, subjectName: subName });
        setMessage('Subject created successfully!');
      }
      setSubCode('');
      setSubName('');
      fetchSubjects();
    } catch (err) {
      setError(err.response?.data?.detail || `Failed to ${editingSubject ? 'update' : 'create'} subject.`);
    } finally {
      setLoading(false);
    }
  };

  const handleEditSubject = (subject) => {
    setEditingSubject(subject);
    setSubCode(subject.subjectCode);
    setSubName(subject.subjectName);
    setError('');
    setMessage('');
  };

  const handleCancelEditSubject = () => {
    setEditingSubject(null);
    setSubCode('');
    setSubName('');
  };

  const handleDeleteSubject = (id) => {
    showConfirm(
      'Delete Subject',
      'Are you sure you want to delete this subject? This action cannot be undone.',
      async () => {
        closeConfirm();
        setError('');
        setMessage('');
        try {
          await api.delete(`/Subjects/${id}`);
          setMessage('Subject deleted successfully!');
          if (editingSubject?.id === id) {
            handleCancelEditSubject();
          }
          fetchSubjects();
        } catch (err) {
          setError(err.response?.data?.detail || 'Failed to delete subject.');
        }
      }
    );
  };

  // Add criteria row
  const addCriteriaRow = () => {
    setCriteria([...criteria, { criteriaName: '', description: '', maxPoints: 10, weight: 10 }]);
  };

  // Remove criteria row
  const removeCriteriaRow = (index) => {
    setCriteria(criteria.filter((_, i) => i !== index));
  };

  // Update criteria cell
  const updateCriteriaCell = (index, field, value) => {
    const updated = criteria.map((c, i) => {
      if (i === index) {
        let val = value;
        if (field === 'maxPoints' || field === 'weight') {
          val = value === '' ? '' : parseFloat(value);
        }
        return { ...c, [field]: val };
      }
      return c;
    });
    setCriteria(updated);
  };

  // Add/Update Rubric
  const handleAddRubric = async (e) => {
    e.preventDefault();
    setError('');
    setMessage('');
    setLoading(true);

    const totalWeight = criteria.reduce((sum, c) => sum + (c.weight || 0), 0);
    if (Math.abs(totalWeight - 1.0) > 0.001 && Math.abs(totalWeight - 100.0) > 0.001) {
      setError(`Total weight must equal 100% or 1.0 (Current: ${totalWeight})`);
      setLoading(false);
      return;
    }

    const criteriaPayload = criteria.map(c => ({
      criteriaName: c.criteriaName,
      description: c.description,
      maxPoints: parseFloat(c.maxPoints),
      weight: parseFloat(c.weight)
    }));

    try {
      if (editingRubricId) {
        await api.put(`/Rubrics/${editingRubricId}`, {
          id: editingRubricId,
          name: rubricName || `Rubric for ${subjects.find(s => s.id === parseInt(selectedSubId))?.subjectCode}`,
          criteria: criteriaPayload
        });
        setMessage('Rubric updated successfully!');
        setEditingRubricId(null);
      } else {
        await api.post('/Rubrics', {
          subjectId: parseInt(selectedSubId),
          name: rubricName || `Rubric for ${subjects.find(s => s.id === parseInt(selectedSubId))?.subjectCode}`,
          criteria: criteriaPayload
        });
        setMessage('Rubric configured successfully!');
      }
      fetchRubric(selectedSubId);
      setRubricName('');
    } catch (err) {
      setError(err.response?.data?.detail || `Failed to ${editingRubricId ? 'update' : 'create'} rubric.`);
    } finally {
      setLoading(false);
    }
  };

  const handleEditRubric = () => {
    if (!currentRubric) return;
    setEditingRubricId(currentRubric.id);
    setRubricName(currentRubric.name);
    setCriteria(currentRubric.criteria.map(c => ({
      criteriaName: c.criteriaName,
      description: c.description,
      maxPoints: c.maxPoints,
      weight: c.weight
    })));
    setError('');
    setMessage('');
  };

  const handleCancelEditRubric = () => {
    setEditingRubricId(null);
    setRubricName('');
    setCriteria([
      { criteriaName: 'Content', description: 'Quality of writing and relevance', maxPoints: 10, weight: 50 },
      { criteriaName: 'Grammar', description: 'Grammar and vocabulary correctness', maxPoints: 10, weight: 50 }
    ]);
  };

  const handleDeleteRubric = () => {
    if (!currentRubric) return;
    showConfirm(
      'Delete Rubric',
      'Are you sure you want to delete this rubric? This action cannot be undone.',
      async () => {
        closeConfirm();
        setError('');
        setMessage('');
        try {
          await api.delete(`/Rubrics/${currentRubric.id}`);
          setMessage('Rubric deleted successfully!');
          setEditingRubricId(null);
          fetchRubric(selectedSubId);
        } catch (err) {
          setError(err.response?.data?.detail || 'Failed to delete rubric.');
        }
      }
    );
  };



  return (
    <div className="dashboard-container fade-in">
      <div className="page-header">
        <div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 700 }}>Academic Staff Portal</h1>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.95rem' }}>Define courses, grading rubrics, and schedule exam slots.</p>
        </div>

        <div className="glass-panel nav-links" style={{ padding: '0.4rem', borderRadius: '10px' }}>
          <button onClick={() => { setActiveTab('subjects'); setError(''); setMessage(''); }} className={`btn btn-sm ${activeTab === 'subjects' ? 'btn-primary' : 'btn-secondary'}`} style={{ border: 'none' }}>
            <BookOpen size={16} /> Subjects
          </button>
          <button onClick={() => { setActiveTab('rubrics'); setError(''); setMessage(''); }} className={`btn btn-sm ${activeTab === 'rubrics' ? 'btn-primary' : 'btn-secondary'}`} style={{ border: 'none' }}>
            <Award size={16} /> Rubrics
          </button>

        </div>
      </div>

      {error && (
        <div style={{ padding: '0.75rem 1rem', background: 'rgba(239, 68, 68, 0.15)', border: '1px solid rgba(239, 68, 68, 0.3)', borderRadius: '8px', color: 'var(--color-danger)', fontSize: '0.9rem', marginBottom: '1.5rem' }}>
          {error}
        </div>
      )}

      {message && (
        <div style={{ padding: '0.75rem 1rem', background: 'rgba(16, 185, 129, 0.15)', border: '1px solid rgba(16, 185, 129, 0.3)', borderRadius: '8px', color: 'var(--color-success)', fontSize: '0.9rem', marginBottom: '1.5rem' }}>
          {message}
        </div>
      )}

      {/* Tab: Subjects */}
      {activeTab === 'subjects' && (
        <div className="grid-cols-2">
          {/* List */}
          <div className="glass-panel" style={{ overflow: 'hidden' }}>
            <div className="card-header">Existing Subjects</div>
            <div style={{ maxHeight: '500px', overflowY: 'auto' }}>
              <table className="custom-table">
                <thead>
                  <tr>
                    <th>Subject Code</th>
                    <th>Subject Name</th>
                    <th style={{ width: '100px', textAlign: 'center' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {subjects.length === 0 ? (
                    <tr>
                      <td colSpan="3" style={{ textAlign: 'center', color: 'var(--text-muted)' }}>No subjects defined yet.</td>
                    </tr>
                  ) : (
                    subjects.map(s => (
                      <tr key={s.id}>
                        <td style={{ fontWeight: 600, color: 'var(--color-primary)' }}>{s.subjectCode}</td>
                        <td>{s.subjectName}</td>
                        <td style={{ textAlign: 'center' }}>
                          <div style={{ display: 'flex', gap: '0.25rem', justifyContent: 'center' }}>
                            <button
                              onClick={() => handleEditSubject(s)}
                              className="btn btn-secondary btn-sm"
                              style={{ padding: '0.3rem 0.5rem' }}
                              title="Edit"
                            >
                              <Edit3 size={14} />
                            </button>
                            <button
                              onClick={() => handleDeleteSubject(s.id)}
                              className="btn btn-danger btn-sm"
                              style={{ padding: '0.3rem 0.5rem' }}
                              title="Delete"
                            >
                              <Trash2 size={14} />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>

          {/* Form */}
          <div className="glass-panel">
            <div className="card-header">{editingSubject ? 'Edit Subject' : 'Add New Subject'}</div>
            <form onSubmit={handleAddSubject} className="card-body">
              <div className="form-group">
                <label className="form-label">Subject Code</label>
                <input
                  type="text"
                  required
                  placeholder="PRN232"
                  className="form-control"
                  value={subCode}
                  onChange={(e) => setSubCode(e.target.value)}
                />
              </div>
              <div className="form-group" style={{ marginBottom: '1.5rem' }}>
                <label className="form-label">Subject Name</label>
                <input
                  type="text"
                  required
                  placeholder="Distributed Applications"
                  className="form-control"
                  value={subName}
                  onChange={(e) => setSubName(e.target.value)}
                />
              </div>
              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <button type="submit" disabled={loading} className="btn btn-primary" style={{ flex: 1 }}>
                  <Plus size={18} /> {loading ? 'Saving...' : (editingSubject ? 'Update Subject' : 'Create Subject')}
                </button>
                {editingSubject && (
                  <button type="button" onClick={handleCancelEditSubject} className="btn btn-secondary">
                    Cancel
                  </button>
                )}
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Tab: Rubrics */}
      {activeTab === 'rubrics' && (
        <div className="grid-cols-2">
          {/* View Rubric */}
          <div className="glass-panel">
            <div className="card-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span>Barem Điểm (Rubric Viewer)</span>
              <select className="form-control" style={{ width: '180px', padding: '0.4rem 0.8rem', fontSize: '0.9rem' }} value={selectedSubId} onChange={handleSubjectChange}>
                {subjects.map(s => (
                  <option key={s.id} value={s.id}>{s.subjectCode}</option>
                ))}
              </select>
            </div>

            <div className="card-body">
              {currentRubric ? (
                <div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                    <h3 style={{ fontSize: '1.25rem', fontWeight: 600, color: 'var(--color-primary)', margin: 0 }}>{currentRubric.name}</h3>
                    <div style={{ display: 'flex', gap: '0.25rem' }}>
                      <button
                        onClick={handleEditRubric}
                        className="btn btn-secondary btn-sm"
                        style={{ padding: '0.3rem 0.5rem' }}
                        title="Edit Rubric"
                      >
                        <Edit3 size={14} />
                      </button>
                      <button
                        onClick={handleDeleteRubric}
                        className="btn btn-danger btn-sm"
                        style={{ padding: '0.3rem 0.5rem' }}
                        title="Delete Rubric"
                      >
                        <Trash2 size={14} />
                      </button>
                    </div>
                  </div>
                  <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', marginBottom: '1.25rem' }}>Tổng trọng số: {currentRubric.totalWeight}%</p>

                  <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                    {currentRubric.criteria.map(c => (
                      <div key={c.id} style={{ background: 'rgba(255,255,255,0.03)', padding: '1rem', border: '1px solid rgba(255,255,255,0.05)', borderRadius: '8px' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.25rem' }}>
                          <span style={{ fontWeight: 600 }}>{c.criteriaName}</span>
                          <span style={{ color: 'var(--color-primary)', fontWeight: 500 }}>{c.weight}% (Max: {c.maxPoints}đ)</span>
                        </div>
                        <p style={{ fontSize: '0.875rem', color: 'var(--text-muted)' }}>{c.description}</p>
                      </div>
                    ))}
                  </div>
                </div>
              ) : (
                <div style={{ textAlign: 'center', padding: '3rem 1rem', color: 'var(--text-muted)' }}>
                  <Award size={48} style={{ strokeWidth: 1, marginBottom: '1rem', color: 'var(--text-muted)' }} />
                  <p>No rubric configured for this subject yet.</p>
                  <p style={{ fontSize: '0.85rem', marginTop: '0.5rem' }}>Configure one in the right panel.</p>
                </div>
              )}
            </div>
          </div>

          {/* Configure Rubric */}
          <div className="glass-panel">
            <div className="card-header">{editingRubricId ? 'Edit Rubric Criteria' : 'Configure Rubric Criteria'}</div>
            <form onSubmit={handleAddRubric} className="card-body">
              <div className="form-group">
                <label className="form-label">Rubric Name</label>
                <input
                  type="text"
                  placeholder="e.g. ENG201 Writing Rubric"
                  className="form-control"
                  value={rubricName}
                  onChange={(e) => setRubricName(e.target.value)}
                />
              </div>

              <div style={{ marginBottom: '1rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ fontSize: '0.9rem', fontWeight: 600, color: 'var(--text-muted)' }}>Criteria Details</span>
                <button type="button" onClick={addCriteriaRow} className="btn btn-secondary btn-sm">
                  <Plus size={14} /> Add Row
                </button>
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', marginBottom: '1.5rem', maxHeight: '350px', overflowY: 'auto' }}>
                {criteria.map((c, index) => (
                  <div key={index} style={{ display: 'flex', gap: '0.5rem', alignItems: 'flex-start', background: 'rgba(255,255,255,0.02)', padding: '0.75rem', borderRadius: '8px' }}>
                    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: '0.4rem' }}>
                      <input
                        type="text"
                        required
                        placeholder="Criteria Name (e.g. Vocabulary)"
                        className="form-control"
                        style={{ padding: '0.4rem 0.8rem', fontSize: '0.9rem' }}
                        value={c.criteriaName}
                        onChange={(e) => updateCriteriaCell(index, 'criteriaName', e.target.value)}
                      />
                      <input
                        type="text"
                        placeholder="Description of criteria"
                        className="form-control"
                        style={{ padding: '0.4rem 0.8rem', fontSize: '0.9rem' }}
                        value={c.description}
                        onChange={(e) => updateCriteriaCell(index, 'description', e.target.value)}
                      />
                    </div>
                    <div style={{ width: '80px' }}>
                      <label style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Max Pts</label>
                      <input
                        type="number"
                        required
                        min="1"
                        className="form-control"
                        style={{ padding: '0.4rem 0.8rem', fontSize: '0.9rem' }}
                        value={c.maxPoints}
                        onChange={(e) => updateCriteriaCell(index, 'maxPoints', e.target.value)}
                      />
                    </div>
                    <div style={{ width: '80px' }}>
                      <label style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Weight (%)</label>
                      <input
                        type="number"
                        required
                        min="1"
                        max="100"
                        className="form-control"
                        style={{ padding: '0.4rem 0.8rem', fontSize: '0.9rem' }}
                        value={c.weight}
                        onChange={(e) => updateCriteriaCell(index, 'weight', e.target.value)}
                      />
                    </div>
                    {criteria.length > 1 && (
                      <button type="button" onClick={() => removeCriteriaRow(index)} className="btn btn-danger btn-sm" style={{ padding: '0.5rem', marginTop: '1.25rem' }}>
                        <Trash2 size={14} />
                      </button>
                    )}
                  </div>
                ))}
              </div>

              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: 'rgba(255,255,255,0.03)', padding: '0.75rem 1rem', borderRadius: '8px', marginBottom: '1.5rem' }}>
                <span style={{ fontSize: '0.9rem' }}>Tổng trọng số:</span>
                <span style={{ fontWeight: 700, color: criteria.reduce((sum, c) => sum + (c.weight || 0), 0) === 100 ? 'var(--color-success)' : 'var(--color-danger)' }}>
                  {criteria.reduce((sum, c) => sum + (c.weight || 0), 0)}% / 100%
                </span>
              </div>

              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <button type="submit" disabled={loading} className="btn btn-primary" style={{ flex: 1 }}>
                  <ClipboardCheck size={18} /> {loading ? 'Saving...' : (editingRubricId ? 'Update Rubric' : 'Save Rubric')}
                </button>
                {editingRubricId && (
                  <button type="button" onClick={handleCancelEditRubric} className="btn btn-secondary">
                    Cancel
                  </button>
                )}
              </div>
            </form>
          </div>
        </div>
      )}


      {/* Confirm Delete Modal */}
      {confirmModal.show && (
        <div className="confirm-overlay" onClick={closeConfirm}>
          <div className="glass-panel confirm-modal" onClick={(e) => e.stopPropagation()}>
            <div style={{ textAlign: 'center', marginBottom: '1.25rem' }}>
              <div style={{ display: 'inline-flex', alignItems: 'center', justifyContent: 'center', width: '48px', height: '48px', borderRadius: '50%', background: 'rgba(239, 68, 68, 0.12)', marginBottom: '0.75rem' }}>
                <AlertTriangle size={24} style={{ color: 'var(--color-danger)' }} />
              </div>
              <h3 style={{ fontSize: '1.15rem', fontWeight: 700, marginBottom: '0.5rem' }}>{confirmModal.title}</h3>
              <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem' }}>{confirmModal.message}</p>
            </div>
            <div style={{ display: 'flex', gap: '0.75rem' }}>
              <button className="btn btn-secondary" style={{ flex: 1 }} onClick={closeConfirm}>Cancel</button>
              <button className="btn btn-danger" style={{ flex: 1 }} onClick={confirmModal.onConfirm}>Delete</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
