import React, { useState, useEffect } from 'react';
import {
  BookOpen,
  Award,
  Layers,
  Plus,
  Trash2,
  ClipboardCheck,
  Edit3,
  AlertTriangle,
  Upload,
  X,
  BarChart2,
  Download
} from 'lucide-react';
import api, { userApi } from '../api';

export default function AcademicDashboard() {
  const [activeTab, setActiveTab] = useState('subjects');

  // Subjects state
  const [subjects, setSubjects] = useState([]);
  const [subCode, setSubCode] = useState('');
  const [subName, setSubName] = useState('');
  const [editingSubject, setEditingSubject] = useState(null);

  // Rubrics state
  const [selectedSubId, setSelectedSubId] = useState('');
  const [rubricName, setRubricName] = useState('');
  const [currentRubric, setCurrentRubric] = useState(null);
  const [editingRubricId, setEditingRubricId] = useState(null);
  const [criteria, setCriteria] = useState([
    { criteriaName: 'Content', description: 'Quality of writing and relevance', maxPoints: 10, weight: 50 },
    { criteriaName: 'Grammar', description: 'Grammar and vocabulary correctness', maxPoints: 10, weight: 50 }
  ]);

  // Master Classes state (Tab 3: Classes)
  const [masterClasses, setMasterClasses] = useState([
    { id: 1, classCode: 'SE1801' },
    { id: 2, classCode: 'SE1802' },
    { id: 3, classCode: 'SE1803' },
    { id: 4, classCode: 'IA1801' }
  ]);
  const [newClassCode, setNewClassCode] = useState('');
  const [editingMasterClass, setEditingMasterClass] = useState(null);

  // Scheduled Exam Classes state (Tab 4: Exam Classes)
  const [classes, setClasses] = useState([]);
  const [lecturers, setLecturers] = useState([]);
  const [selectedClassMasterId, setSelectedClassMasterId] = useState('');
  const [classSubjectId, setClassSubjectId] = useState('');
  const [semester, setSemester] = useState('SU26');
  const [lecturerId, setLecturerId] = useState('');
  const [zipFile, setZipFile] = useState(null);

  // Analytics state
  const [analyticsClassId, setAnalyticsClassId] = useState('');
  const [analyticsData, setAnalyticsData] = useState(null);
  const [analyticsLoading, setAnalyticsLoading] = useState(false);

  // Common state
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [confirmModal, setConfirmModal] = useState({
    show: false,
    title: '',
    message: '',
    onConfirm: null
  });

  const showConfirm = (title, msg, onConfirm) => {
    setConfirmModal({ show: true, title, message: msg, onConfirm });
  };

  const closeConfirm = () => {
    setConfirmModal({ show: false, title: '', message: '', onConfirm: null });
  };

  useEffect(() => {
    fetchSubjects();
    fetchClasses();
    fetchLecturers();
    fetchMasterClasses();
  }, []);

  // Load local saved master classes if available
  useEffect(() => {
    const saved = localStorage.getItem('masterClasses');
    if (saved) {
      try {
        setMasterClasses(JSON.parse(saved));
      } catch (e) {
        console.error(e);
      }
    }
  }, []);

  const saveMasterClassesToStorage = (updated) => {
    setMasterClasses(updated);
    localStorage.setItem('masterClasses', JSON.stringify(updated));
  };

  // =========================
  // ANALYTICS & EXPORT
  // =========================
  const fetchAnalytics = async (examClassId) => {
    if (!examClassId) return;
    setAnalyticsLoading(true);
    setAnalyticsData(null);
    try {
      const res = await api.get(`/ExamClasses/${examClassId}/analytics`);
      setAnalyticsData(res.data);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load analytics.');
    } finally {
      setAnalyticsLoading(false);
    }
  };

  const handleExportExcel = async (examClassId) => {
    try {
      const res = await api.get(`/ExamClasses/${examClassId}/export-excel`, {
        responseType: 'blob'
      });
      const contentDisposition = res.headers['content-disposition'] || '';
      const match = contentDisposition.match(/filename[^;=\n]*=([^;\n]*)/);
      const fileName = match ? match[1].replace(/["']/g, '').trim() : `BangDiem_${examClassId}.xlsx`;
      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (err) {
      setError('Failed to export Excel. Make sure the exam class has graded submissions.');
    }
  };

  // =========================
  // SUBJECTS HANDLERS
  // =========================
  const fetchSubjects = async () => {
    try {
      const res = await api.get('/Subjects');
      setSubjects(res.data);
      if (res.data.length > 0 && !selectedSubId) {
        const firstSubjectId = res.data[0].id.toString();
        setSelectedSubId(firstSubjectId);
        fetchRubric(firstSubjectId);
      }
      if (res.data.length > 0 && !classSubjectId) {
        setClassSubjectId(res.data[0].id.toString());
      }
    } catch (err) {
      console.error(err);
    }
  };

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
      setError(err.response?.data?.detail || err.response?.data?.message || 'Failed to save subject.');
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
          fetchSubjects();
        } catch (err) {
          setError(err.response?.data?.detail || 'Failed to delete subject.');
        }
      }
    );
  };

  // =========================
  // RUBRICS HANDLERS
  // =========================
  const fetchRubric = async (subId) => {
    try {
      setCurrentRubric(null);
      const res = await api.get(`/Rubrics/subject/${subId}`);
      setCurrentRubric(res.data);
    } catch (err) {
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

  const addCriteriaRow = () => {
    setCriteria([...criteria, { criteriaName: '', description: '', maxPoints: 10, weight: 10 }]);
  };

  const removeCriteriaRow = (index) => {
    setCriteria(criteria.filter((_, i) => i !== index));
  };

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

  const handleAddRubric = async (e) => {
    e.preventDefault();
    setError('');
    setMessage('');
    setLoading(true);

    const totalWeight = criteria.reduce((sum, c) => sum + (Number(c.weight) || 0), 0);
    if (Math.abs(totalWeight - 1.0) > 0.001 && Math.abs(totalWeight - 100.0) > 0.001) {
      setError(`Total weight must equal 100% or 1.0 (Current: ${totalWeight}%)`);
      setLoading(false);
      return;
    }

    const criteriaPayload = criteria.map((c) => ({
      criteriaName: c.criteriaName,
      description: c.description,
      maxPoints: parseFloat(c.maxPoints),
      weight: parseFloat(c.weight)
    }));

    try {
      if (editingRubricId) {
        await api.put(`/Rubrics/${editingRubricId}`, {
          id: editingRubricId,
          name: rubricName || `Rubric for ${subjects.find((s) => s.id === parseInt(selectedSubId))?.subjectCode}`,
          criteria: criteriaPayload
        });
        setMessage('Rubric updated successfully!');
        setEditingRubricId(null);
      } else {
        await api.post('/Rubrics', {
          subjectId: parseInt(selectedSubId),
          name: rubricName || `Rubric for ${subjects.find((s) => s.id === parseInt(selectedSubId))?.subjectCode}`,
          criteria: criteriaPayload
        });
        setMessage('Rubric configured successfully!');
      }
      fetchRubric(selectedSubId);
      setRubricName('');
    } catch (err) {
      setError(err.response?.data?.detail || 'Failed to save rubric.');
    } finally {
      setLoading(false);
    }
  };

  const handleEditRubric = () => {
    if (!currentRubric) return;
    setEditingRubricId(currentRubric.id);
    setRubricName(currentRubric.name);
    setCriteria(
      currentRubric.criteria.map((c) => ({
        criteriaName: c.criteriaName,
        description: c.description,
        maxPoints: c.maxPoints,
        weight: c.weight
      }))
    );
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

  // =========================
  // MASTER CLASSES HANDLERS (Tab 3: Classes)
  // =========================
  const fetchMasterClasses = async () => {
    try {
      const res = await api.get('/Classes');
      if (res.data && Array.isArray(res.data) && res.data.length > 0) {
        setMasterClasses(res.data);
      }
    } catch (err) {
      console.log('Using local masterClasses catalogue.');
    }
  };

  const handleAddMasterClass = async (e) => {
    e.preventDefault();
    if (!newClassCode.trim()) return;
    setError('');
    setMessage('');
    setLoading(true);

    const formattedCode = newClassCode.trim().toUpperCase();

    try {
      try {
        if (editingMasterClass) {
          await api.put(`/Classes/${editingMasterClass.id}`, { id: editingMasterClass.id, classCode: formattedCode });
        } else {
          await api.post('/Classes', { classCode: formattedCode });
        }
        fetchMasterClasses();
      } catch (err) {
        if (editingMasterClass) {
          const updated = masterClasses.map(c => c.id === editingMasterClass.id ? { ...c, classCode: formattedCode } : c);
          saveMasterClassesToStorage(updated);
        } else {
          const newObj = { id: Date.now(), classCode: formattedCode };
          saveMasterClassesToStorage([...masterClasses, newObj]);
        }
      }

      setMessage(editingMasterClass ? 'Class updated successfully!' : 'Class created successfully!');
      setNewClassCode('');
      setEditingMasterClass(null);
    } catch (err) {
      setError('Failed to save class.');
    } finally {
      setLoading(false);
    }
  };

  const handleEditMasterClass = (cls) => {
    setEditingMasterClass(cls);
    setNewClassCode(cls.classCode);
    setError('');
    setMessage('');
  };

  const handleCancelEditMasterClass = () => {
    setEditingMasterClass(null);
    setNewClassCode('');
  };

  const handleDeleteMasterClass = (id) => {
    showConfirm(
      'Delete Class',
      'Are you sure you want to delete this class code? This action cannot be undone.',
      async () => {
        closeConfirm();
        try {
          await api.delete(`/Classes/${id}`);
          fetchMasterClasses();
        } catch (err) {
          const updated = masterClasses.filter(c => c.id !== id);
          saveMasterClassesToStorage(updated);
        }
        setMessage('Class deleted successfully!');
      }
    );
  };

  // =========================
  // EXAM CLASSES HANDLERS (Tab 4: Exam Classes)
  // =========================
  const fetchClasses = async () => {
    try {
      const res = await api.get('/ExamClasses');
      setClasses(res.data);
    } catch (err) {
      console.error(err);
    }
  };

  const fetchLecturers = async () => {
    try {
      const res = await userApi.get('/Auth/lecturers');
      setLecturers(res.data);
      if (res.data.length > 0 && !lecturerId) {
        setLecturerId(res.data[0].id.toString());
      }
    } catch (err) {
      console.error(err);
    }
  };

  const resetClassForm = () => {
    setSelectedClassMasterId('');
    setClassSubjectId(subjects[0]?.id ? subjects[0].id.toString() : '');
    setSemester('SU26');
    setLecturerId(lecturers[0]?.id ? lecturers[0].id.toString() : '');
    setZipFile(null);
  };

  const handleSubmitClass = async (e) => {
    e.preventDefault();
    setError('');
    setMessage('');
    setLoading(true);

    if (!classSubjectId) {
      setError('Please select a subject.');
      setLoading(false);
      return;
    }

    const selectedMaster = masterClasses.find(c => c.id.toString() === selectedClassMasterId.toString() || c.classCode === selectedClassMasterId);
    const targetClassCode = selectedMaster ? selectedMaster.classCode : (selectedClassMasterId || masterClasses[0]?.classCode || 'SE1801');

    try {
      const payload = {
        classCode: targetClassCode,
        subjectId: parseInt(classSubjectId),
        semester: semester.trim(),
        lecturerId: lecturerId ? parseInt(lecturerId) : null
      };

      const res = await api.post('/ExamClasses', payload);
      const newExamClassId = res.data?.id || res.data;

      if (zipFile && newExamClassId) {
        const formData = new FormData();
        formData.append('file', zipFile);
        try {
          await api.post(`/ExamClasses/${newExamClassId}/upload-zip`, formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
          });
        } catch (uploadErr) {
          console.error('ZIP Upload failed:', uploadErr);
        }
      }

      setMessage('Exam class created successfully!');
      resetClassForm();
      fetchClasses();
    } catch (err) {
      setError(err.response?.data?.detail || err.response?.data?.message || 'Failed to create exam class.');
    } finally {
      setLoading(false);
    }
  };

  const getLecturerDisplayName = (l) => l.fullName || l.username || `Lecturer #${l.id}`;

  const totalCriteriaWeight = criteria.reduce((sum, c) => sum + (Number(c.weight) || 0), 0);

  return (
    <div className="dashboard-container fade-in">
      {/* Header & Tabs */}
      <div className="page-header">
        <div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 700 }}>Academic Staff Portal</h1>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.95rem' }}>Define courses, grading rubrics, master classes, and schedule exam slots.</p>
        </div>

        <div className="glass-panel nav-links" style={{ padding: '0.4rem', borderRadius: '10px' }}>
          <button
            onClick={() => { setActiveTab('subjects'); setError(''); setMessage(''); }}
            className={`btn btn-sm ${activeTab === 'subjects' ? 'btn-primary' : 'btn-secondary'}`}
            style={{ border: 'none' }}
          >
            <BookOpen size={16} /> Subjects
          </button>

          <button
            onClick={() => { setActiveTab('rubrics'); setError(''); setMessage(''); }}
            className={`btn btn-sm ${activeTab === 'rubrics' ? 'btn-primary' : 'btn-secondary'}`}
            style={{ border: 'none' }}
          >
            <Award size={16} /> Rubrics
          </button>

          <button
            onClick={() => { setActiveTab('masterClasses'); setError(''); setMessage(''); }}
            className={`btn btn-sm ${activeTab === 'masterClasses' ? 'btn-primary' : 'btn-secondary'}`}
            style={{ border: 'none' }}
          >
            <Layers size={16} /> Classes
          </button>

          <button
            onClick={() => { setActiveTab('classes'); setError(''); setMessage(''); }}
            className={`btn btn-sm ${activeTab === 'classes' ? 'btn-primary' : 'btn-secondary'}`}
            style={{ border: 'none' }}
          >
            <ClipboardCheck size={16} /> Exam Classes
          </button>

          <button
            onClick={() => { setActiveTab('analytics'); setError(''); setMessage(''); }}
            className={`btn btn-sm ${activeTab === 'analytics' ? 'btn-primary' : 'btn-secondary'}`}
            style={{ border: 'none' }}
          >
            <BarChart2 size={16} /> Analytics
          </button>
        </div>
      </div>

      {/* Global Alerts */}
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

      {/* Tab 1: Subjects */}
      {activeTab === 'subjects' && (
        <div className="grid-cols-2">
          <div className="glass-panel" style={{ overflow: 'hidden' }}>
            <div className="card-header">Existing Subjects</div>
            <div style={{ maxHeight: '500px', overflowY: 'auto' }}>
              <table className="custom-table">
                <thead>
                  <tr>
                    <th>Subject Code</th>
                    <th>Subject Name</th>
                    <th style={{ width: '120px', textAlign: 'center' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {subjects.length === 0 ? (
                    <tr>
                      <td colSpan="3" style={{ textAlign: 'center', color: 'var(--text-muted)', padding: '2rem' }}>No subjects defined yet.</td>
                    </tr>
                  ) : (
                    subjects.map((s) => (
                      <tr key={s.id}>
                        <td style={{ fontWeight: 600, color: 'var(--color-primary)' }}>{s.subjectCode}</td>
                        <td>{s.subjectName}</td>
                        <td style={{ textAlign: 'center' }}>
                          <div style={{ display: 'flex', gap: '0.25rem', justifyContent: 'center' }}>
                            <button onClick={() => handleEditSubject(s)} className="btn btn-secondary btn-sm" style={{ padding: '0.3rem 0.5rem' }} title="Edit">
                              <Edit3 size={14} />
                            </button>
                            <button onClick={() => handleDeleteSubject(s.id)} className="btn btn-danger btn-sm" style={{ padding: '0.3rem 0.5rem' }} title="Delete">
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

          <div className="glass-panel">
            <div className="card-header">{editingSubject ? 'Edit Subject' : 'Add New Subject'}</div>
            <form onSubmit={handleAddSubject} className="card-body">
              <div className="form-group">
                <label className="form-label">Subject Code</label>
                <input type="text" required placeholder="PRN232" className="form-control" value={subCode} onChange={(e) => setSubCode(e.target.value)} />
              </div>
              <div className="form-group" style={{ marginBottom: '1.5rem' }}>
                <label className="form-label">Subject Name</label>
                <input type="text" required placeholder="Distributed Applications" className="form-control" value={subName} onChange={(e) => setSubName(e.target.value)} />
              </div>
              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <button type="submit" disabled={loading} className="btn btn-primary" style={{ flex: 1 }}>
                  <Plus size={18} /> {loading ? 'Saving...' : editingSubject ? 'Update Subject' : 'Create Subject'}
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

      {/* Tab 2: Rubrics */}
      {activeTab === 'rubrics' && (
        <div className="grid-cols-2">
          <div className="glass-panel">
            <div className="card-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span>Barem Điểm (Rubric Viewer)</span>
              <select className="form-control" style={{ width: '180px', padding: '0.4rem 0.8rem', fontSize: '0.9rem' }} value={selectedSubId} onChange={handleSubjectChange}>
                {subjects.map((s) => (
                  <option key={s.id} value={s.id}>{s.subjectCode}</option>
                ))}
              </select>
            </div>
            <div className="card-body">
              {currentRubric ? (
                <div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                    <h3 style={{ fontSize: '1.25rem', fontWeight: 600, color: 'var(--color-primary)' }}>{currentRubric.name}</h3>
                    <div style={{ display: 'flex', gap: '0.5rem' }}>
                      <button onClick={handleEditRubric} className="btn btn-secondary btn-sm" title="Edit Rubric">
                        <Edit3 size={14} /> Edit
                      </button>
                      <button onClick={handleDeleteRubric} className="btn btn-danger btn-sm" title="Delete Rubric">
                        <Trash2 size={14} /> Delete
                      </button>
                    </div>
                  </div>
                  <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', marginBottom: '1.25rem' }}>Tổng trọng số: {currentRubric.totalWeight}%</p>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                    {currentRubric.criteria.map((c) => (
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

          <div className="glass-panel">
            <div className="card-header">{editingRubricId ? 'Edit Rubric Criteria' : 'Configure Rubric Criteria'}</div>
            <form onSubmit={handleAddRubric} className="card-body">
              <div className="form-group">
                <label className="form-label">Rubric Name</label>
                <input type="text" placeholder="e.g. ENG201 Writing Rubric" className="form-control" value={rubricName} onChange={(e) => setRubricName(e.target.value)} />
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
                      <input type="text" required placeholder="Criteria Name" className="form-control" style={{ padding: '0.4rem 0.8rem', fontSize: '0.9rem' }} value={c.criteriaName} onChange={(e) => updateCriteriaCell(index, 'criteriaName', e.target.value)} />
                      <input type="text" placeholder="Description" className="form-control" style={{ padding: '0.4rem 0.8rem', fontSize: '0.9rem' }} value={c.description} onChange={(e) => updateCriteriaCell(index, 'description', e.target.value)} />
                    </div>
                    <div style={{ width: '80px' }}>
                      <label style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Max Pts</label>
                      <input type="number" required min="1" className="form-control" style={{ padding: '0.4rem 0.8rem', fontSize: '0.9rem' }} value={c.maxPoints} onChange={(e) => updateCriteriaCell(index, 'maxPoints', e.target.value)} />
                    </div>
                    <div style={{ width: '80px' }}>
                      <label style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Weight (%)</label>
                      <input type="number" required min="1" max="100" className="form-control" style={{ padding: '0.4rem 0.8rem', fontSize: '0.9rem' }} value={c.weight} onChange={(e) => updateCriteriaCell(index, 'weight', e.target.value)} />
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
                <span style={{ fontWeight: 700, color: totalCriteriaWeight === 100 ? 'var(--color-success)' : 'var(--color-danger)' }}>
                  {totalCriteriaWeight}% / 100%
                </span>
              </div>
              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <button type="submit" disabled={loading} className="btn btn-primary" style={{ flex: 1 }}>
                  <ClipboardCheck size={18} /> {loading ? 'Saving...' : editingRubricId ? 'Update Rubric' : 'Save Rubric'}
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

      {/* Tab 3: Classes (Master Classes CRUD) */}
      {activeTab === 'masterClasses' && (
        <div className="grid-cols-2">
          <div className="glass-panel" style={{ overflow: 'hidden' }}>
            <div className="card-header">All Classes (Danh mục mã Lớp)</div>
            <div style={{ maxHeight: '560px', overflowY: 'auto' }}>
              <table className="custom-table">
                <thead>
                  <tr>
                    <th>Class Code</th>
                    <th style={{ width: '120px', textAlign: 'center' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {masterClasses.length === 0 ? (
                    <tr>
                      <td colSpan="2" style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-muted)' }}>
                        No classes found. Add one on the right panel.
                      </td>
                    </tr>
                  ) : (
                    masterClasses.map((c) => (
                      <tr key={c.id}>
                        <td style={{ fontWeight: 600, color: 'var(--color-secondary)' }}>{c.classCode}</td>
                        <td style={{ textAlign: 'center' }}>
                          <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'center' }}>
                            <button onClick={() => handleEditMasterClass(c)} className="btn btn-secondary btn-sm" style={{ padding: '0.3rem 0.5rem' }} title="Edit">
                              <Edit3 size={14} />
                            </button>
                            <button onClick={() => handleDeleteMasterClass(c.id)} className="btn btn-danger btn-sm" style={{ padding: '0.3rem 0.5rem' }} title="Delete">
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

          <div className="glass-panel">
            <div className="card-header">{editingMasterClass ? 'Edit Class Code' : 'Create Class Code (Tạo Mã Lớp Trước)'}</div>
            <form onSubmit={handleAddMasterClass} className="card-body">
              <div className="form-group">
                <label className="form-label">Class Code</label>
                <input type="text" required placeholder="e.g. SE1801, SE1802..." className="form-control" value={newClassCode} onChange={(e) => setNewClassCode(e.target.value)} />
              </div>
              <div style={{ display: 'flex', gap: '0.5rem', marginTop: '1.5rem' }}>
                <button type="submit" disabled={loading} className="btn btn-primary" style={{ flex: 1 }}>
                  {editingMasterClass ? 'Save Changes' : 'Create Class'}
                </button>
                {editingMasterClass && (
                  <button type="button" onClick={handleCancelEditMasterClass} className="btn btn-secondary" style={{ flex: 0.5 }}>
                    Cancel
                  </button>
                )}
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Tab 4: Exam Classes (Schedule & Assign - SELECT ONLY) */}
      {activeTab === 'classes' && (
        <div className="grid-cols-2">
          <div className="glass-panel" style={{ overflow: 'hidden' }}>
            <div className="card-header">Exam Classes (Danh sách Lớp thi đã xếp)</div>
            <div style={{ maxHeight: '560px', overflowY: 'auto' }}>
              <table className="custom-table">
                <thead>
                  <tr>
                    <th>Class</th>
                    <th>Subject</th>
                    <th>Semester</th>
                    <th>Lecturer</th>
                    <th>Status</th>
                    <th style={{ textAlign: 'center' }}>Excel</th>
                  </tr>
                </thead>
                <tbody>
                  {classes.length === 0 ? (
                    <tr>
                      <td colSpan="6" style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-muted)' }}>
                        No exam classes created yet.
                      </td>
                    </tr>
                  ) : (
                    classes.map((c) => (
                      <tr key={c.id}>
                        <td style={{ fontWeight: 600, color: 'var(--color-secondary)' }}>{c.classCode}</td>
                        <td>{c.subjectCode}</td>
                        <td>{c.semester}</td>
                        <td>{c.lecturerName}</td>
                        <td>
                          <span className={`status-pill ${c.status.toLowerCase()}`}>
                            {c.status}
                          </span>
                        </td>
                        <td style={{ textAlign: 'center' }}>
                          <button onClick={() => handleExportExcel(c.id)} className="btn btn-secondary btn-sm" style={{ padding: '0.3rem 0.5rem' }} title="Export Excel">
                            <Download size={14} />
                          </button>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>

          <div className="glass-panel">
            <div className="card-header">Create Exam Class (Xếp Lịch Thi & Gán Giảng Viên)</div>
            <form onSubmit={handleSubmitClass} className="card-body">
              {/* SELECT Class Code from Master Classes */}
              <div className="form-group">
                <label className="form-label">Class Code (Select from Classes tab)</label>
                <select className="form-control" value={selectedClassMasterId} onChange={(e) => setSelectedClassMasterId(e.target.value)} required>
                  <option value="">-- Select Class Code --</option>
                  {masterClasses.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.classCode}
                    </option>
                  ))}
                </select>
              </div>

              {/* SELECT Subject */}
              <div className="form-group">
                <label className="form-label">Subject</label>
                <select className="form-control" value={classSubjectId} onChange={(e) => setClassSubjectId(e.target.value)} required>
                  <option value="">-- Select Subject --</option>
                  {subjects.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.subjectCode} - {s.subjectName}
                    </option>
                  ))}
                </select>
              </div>

              {/* INPUT Semester */}
              <div className="form-group">
                <label className="form-label">Semester</label>
                <input type="text" required placeholder="SU26" className="form-control" value={semester} onChange={(e) => setSemester(e.target.value)} />
              </div>

              {/* SELECT Lecturer */}
              <div className="form-group">
                <label className="form-label">Lecturer</label>
                <select className="form-control" value={lecturerId} onChange={(e) => setLecturerId(e.target.value)} required>
                  <option value="">-- Select Lecturer --</option>
                  {lecturers.map((l) => (
                    <option key={l.id} value={l.id}>
                      {getLecturerDisplayName(l)}
                    </option>
                  ))}
                </select>
              </div>

              {/* ZIP File upload (optional) */}
              <div className="form-group">
                <label className="form-label">Submissions ZIP (optional)</label>
                <div
                  style={{
                    border: '2px dashed var(--panel-border)',
                    borderRadius: '0.5rem',
                    padding: '1rem',
                    textAlign: 'center',
                    cursor: 'pointer',
                    background: zipFile ? 'rgba(242,113,33,0.05)' : 'transparent',
                    transition: 'all 0.2s'
                  }}
                  onClick={() => document.getElementById('zip-file-input').click()}
                >
                  <Upload size={20} style={{ color: 'var(--text-muted)', marginBottom: '0.25rem' }} />
                  {zipFile ? (
                    <div>
                      <div style={{ fontWeight: 600, fontSize: '0.9rem' }}>{zipFile.name}</div>
                      <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{(zipFile.size / 1024 / 1024).toFixed(2)} MB</div>
                      <button
                        type="button"
                        onClick={(e) => { e.stopPropagation(); setZipFile(null); }}
                        style={{ marginTop: '0.35rem', fontSize: '0.78rem', color: 'var(--color-danger)', background: 'none', border: 'none', cursor: 'pointer', textDecoration: 'underline' }}
                      >
                        Remove file
                      </button>
                    </div>
                  ) : (
                    <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Click to select a .zip file</div>
                  )}
                  <input id="zip-file-input" type="file" accept=".zip" style={{ display: 'none' }} onChange={(e) => { const f = e.target.files?.[0]; if (f) setZipFile(f); }} />
                </div>
              </div>

              <button type="submit" disabled={loading} className="btn btn-primary" style={{ width: '100%', marginTop: '1rem' }}>
                <ClipboardCheck size={18} /> {loading ? 'Creating...' : 'Create Exam Class'}
              </button>
            </form>
          </div>
        </div>
      )}

      {/* Tab 5: Analytics */}
      {activeTab === 'analytics' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
          <div className="glass-panel">
            <div className="card-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span>Exam Class Analytics & Performance Reports</span>
              <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                <select className="form-control" style={{ width: '220px', padding: '0.4rem 0.8rem', fontSize: '0.9rem' }} value={analyticsClassId} onChange={(e) => { setAnalyticsClassId(e.target.value); fetchAnalytics(e.target.value); }}>
                  <option value="">-- Select Exam Class --</option>
                  {classes.map((c) => (
                    <option key={c.id} value={c.id}>{c.classCode} ({c.subjectCode})</option>
                  ))}
                </select>
                {analyticsClassId && (
                  <button onClick={() => handleExportExcel(analyticsClassId)} className="btn btn-secondary btn-sm" title="Export Excel">
                    <Download size={16} /> Export Excel
                  </button>
                )}
              </div>
            </div>
            <div className="card-body">
              {!analyticsClassId ? (
                <div style={{ textAlign: 'center', padding: '3rem 1rem', color: 'var(--text-muted)' }}>
                  <BarChart2 size={48} style={{ strokeWidth: 1, marginBottom: '1rem' }} />
                  <p>Select an Exam Class above to view analytics and score distribution.</p>
                </div>
              ) : analyticsLoading ? (
                <div style={{ textAlign: 'center', padding: '3rem 1rem', color: 'var(--text-muted)' }}>Loading analytics...</div>
              ) : analyticsData ? (
                <div className="grid-cols-2">
                  <div style={{ background: 'rgba(255,255,255,0.02)', padding: '1.25rem', borderRadius: '8px', border: '1px solid var(--panel-border)' }}>
                    <h4 style={{ fontSize: '1rem', fontWeight: 600, marginBottom: '1rem', color: 'var(--color-primary)' }}>Overview Metrics</h4>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                        <span style={{ color: 'var(--text-muted)' }}>Total Submissions:</span>
                        <span style={{ fontWeight: 600 }}>{analyticsData.totalSubmissions}</span>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                        <span style={{ color: 'var(--text-muted)' }}>Graded Submissions:</span>
                        <span style={{ fontWeight: 600 }}>{analyticsData.gradedCount}</span>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                        <span style={{ color: 'var(--text-muted)' }}>Average Score:</span>
                        <span style={{ fontWeight: 700, color: 'var(--color-primary)' }}>{analyticsData.averageScore ? analyticsData.averageScore.toFixed(2) : '-'}</span>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                        <span style={{ color: 'var(--text-muted)' }}>Pass Rate:</span>
                        <span style={{ fontWeight: 700, color: 'var(--color-success)' }}>{analyticsData.passRate ? `${analyticsData.passRate.toFixed(1)}%` : '-'}</span>
                      </div>
                    </div>
                  </div>

                  <div style={{ background: 'rgba(255,255,255,0.02)', padding: '1.25rem', borderRadius: '8px', border: '1px solid var(--panel-border)' }}>
                    <h4 style={{ fontSize: '1rem', fontWeight: 600, marginBottom: '1rem', color: 'var(--color-primary)' }}>Score Breakdown</h4>
                    <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Class analytics and automated report ready for download.</p>
                    <button onClick={() => handleExportExcel(analyticsClassId)} className="btn btn-primary" style={{ width: '100%', marginTop: '1.5rem' }}>
                      <Download size={16} /> Download Full Class Grade Sheet (.xlsx)
                    </button>
                  </div>
                </div>
              ) : (
                <div style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-muted)' }}>No data available for this class.</div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Confirmation Modal */}
      {confirmModal.show && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', backdropFilter: 'blur(4px)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div className="glass-panel fade-in" style={{ width: '100%', maxWidth: '420px', padding: '1.5rem', border: '1px solid var(--panel-border)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontWeight: 700, fontSize: '1.1rem', color: 'var(--color-danger)' }}>
                <AlertTriangle size={20} /> {confirmModal.title}
              </div>
              <button onClick={closeConfirm} style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}>
                <X size={18} />
              </button>
            </div>
            <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', marginBottom: '1.5rem' }}>{confirmModal.message}</p>
            <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'flex-end' }}>
              <button onClick={closeConfirm} className="btn btn-secondary btn-sm">Cancel</button>
              <button onClick={confirmModal.onConfirm} className="btn btn-danger btn-sm">Confirm Delete</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}