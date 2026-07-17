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
import * as signalR from '@microsoft/signalr';
import api from '../api';

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
    {
      criteriaName: 'Content',
      description: 'Quality of writing and relevance',
      maxPoints: 10,
      weight: 50
    },
    {
      criteriaName: 'Grammar',
      description: 'Grammar and vocabulary correctness',
      maxPoints: 10,
      weight: 50
    }
  ]);

  // Exam Classes state
  const [classes, setClasses] = useState([]);
  const [lecturers, setLecturers] = useState([]);
  const [editingClass, setEditingClass] = useState(null);

  const [classCode, setClassCode] = useState(''); // Will store classId (string)
  const [classSubjectId, setClassSubjectId] = useState('');
  const [semester, setSemester] = useState('SU26');
  const [lecturerId, setLecturerId] = useState('');
  const [zipFile, setZipFile] = useState(null);

  // Master Classes state
  const [masterClasses, setMasterClasses] = useState([]);
  const [newClassCode, setNewClassCode] = useState('');
  const [editingMasterClass, setEditingMasterClass] = useState(null);


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
    setConfirmModal({
      show: true,
      title,
      message: msg,
      onConfirm
    });
  };

  const closeConfirm = () => {
    setConfirmModal({
      show: false,
      title: '',
      message: '',
      onConfirm: null
    });
  };

  useEffect(() => {
    fetchSubjects();
    fetchClasses();
    fetchLecturers();
    fetchMasterClasses();
  }, []);

  // =========================
  // ANALYTICS
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

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5257/notificationHub')
      .withAutomaticReconnect()
      .build();

    connection.start()
      .then(() => {
        console.log('SignalR connected to NotificationHub.');
        
        connection.on('ReceiveExamClassStatus', (examClassId, status, submissionCount) => {
          console.log(`SignalR: ExamClass ${examClassId} status updated to ${status} with ${submissionCount} submissions.`);
          setClasses(prevClasses => 
            prevClasses.map(c => 
              c.id === examClassId 
                ? { ...c, status, submissionCount } 
                : c
            )
          );
        });
      })
      .catch(err => console.error('SignalR connection error: ', err));

    return () => {
      connection.stop();
    };
  }, []);

  // =========================
  // SUBJECTS
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
        await api.post('/Subjects', {
          subjectCode: subCode,
          subjectName: subName
        });

        setMessage('Subject created successfully!');
      }

      setSubCode('');
      setSubName('');
      fetchSubjects();
    } catch (err) {
      setError(
        err.response?.data?.detail ||
          err.response?.data?.message ||
          `Failed to ${editingSubject ? 'update' : 'create'} subject.`
      );
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
          setError(
            err.response?.data?.detail ||
              err.response?.data?.message ||
              'Failed to delete subject.'
          );
        }
      }
    );
  };

  // =========================
  // RUBRICS
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
    setCriteria([
      ...criteria,
      {
        criteriaName: '',
        description: '',
        maxPoints: 10,
        weight: 10
      }
    ]);
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

        return {
          ...c,
          [field]: val
        };
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

    const totalWeight = criteria.reduce(
      (sum, c) => sum + (Number(c.weight) || 0),
      0
    );

    if (
      Math.abs(totalWeight - 1.0) > 0.001 &&
      Math.abs(totalWeight - 100.0) > 0.001
    ) {
      setError(`Total weight must equal 100% or 1.0 (Current: ${totalWeight})`);
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
          name:
            rubricName ||
            `Rubric for ${
              subjects.find((s) => s.id === parseInt(selectedSubId))?.subjectCode
            }`,
          criteria: criteriaPayload
        });

        setMessage('Rubric updated successfully!');
        setEditingRubricId(null);
      } else {
        await api.post('/Rubrics', {
          subjectId: parseInt(selectedSubId),
          name:
            rubricName ||
            `Rubric for ${
              subjects.find((s) => s.id === parseInt(selectedSubId))?.subjectCode
            }`,
          criteria: criteriaPayload
        });

        setMessage('Rubric configured successfully!');
      }

      fetchRubric(selectedSubId);
      setRubricName('');
    } catch (err) {
      setError(
        err.response?.data?.detail ||
          err.response?.data?.message ||
          `Failed to ${editingRubricId ? 'update' : 'create'} rubric.`
      );
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
      {
        criteriaName: 'Content',
        description: 'Quality of writing and relevance',
        maxPoints: 10,
        weight: 50
      },
      {
        criteriaName: 'Grammar',
        description: 'Grammar and vocabulary correctness',
        maxPoints: 10,
        weight: 50
      }
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
          setError(
            err.response?.data?.detail ||
              err.response?.data?.message ||
              'Failed to delete rubric.'
          );
        }
      }
    );
  };

  // =========================
  // EXAM CLASSES
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
      const res = await api.get('/Auth/lecturers');
      setLecturers(res.data);
    } catch (err) {
      console.error(err);
    }
  };

  const fetchMasterClasses = async () => {
    try {
      const res = await api.get('/Classes');
      setMasterClasses(res.data);
    } catch (err) {
      console.error(err);
    }
  };


  const resetClassForm = () => {
    setEditingClass(null);
    setClassCode('');
    setClassSubjectId('');
    setSemester('SU26');
    setLecturerId('');
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

    if (!classCode) {
      setError('Please select a class.');
      setLoading(false);
      return;
    }

    if (!lecturerId) {
      setError('Please select a lecturer.');
      setLoading(false);
      return;
    }

    try {
      const payload = {
        classId: parseInt(classCode),
        subjectId: parseInt(classSubjectId),
        semester: semester.trim(),
        lecturerId: parseInt(lecturerId)
      };

      const res = await api.post('/ExamClasses', payload);
      const newExamClassId = res.data;

      // Upload ZIP if a file was selected
      if (zipFile && newExamClassId) {
        const formData = new FormData();
        formData.append('file', zipFile);

        await api.post(
          `/ExamClasses/${newExamClassId}/upload-zip`,
          formData,
          {
            headers: {
              'Content-Type': 'multipart/form-data'
            }
          }
        );
      }

      setMessage(
        zipFile
          ? 'Exam class created and submissions uploaded successfully!'
          : 'Exam class created successfully!'
      );

      resetClassForm();
      fetchClasses();
    } catch (err) {
      setError(
        err.response?.data?.detail ||
          err.response?.data?.message ||
          'Failed to create exam class.'
      );
    } finally {
      setLoading(false);
    }
  };


  const getLecturerDisplayName = (lecturer) => {
    return (
      lecturer.fullName ||
      lecturer.username ||
      lecturer.email ||
      `Lecturer #${lecturer.id}`
    );
  };

  const getClassSubjectName = (examClass) => {
    if (examClass.subjectCode || examClass.subjectName) {
      return `${examClass.subjectCode || ''}${
        examClass.subjectName ? ` - ${examClass.subjectName}` : ''
      }`;
    }

    const subject = subjects.find((s) => s.id === examClass.subjectId);

    if (!subject) return examClass.subjectId || '-';

    return `${subject.subjectCode} - ${subject.subjectName}`;
  };

  const getClassLecturerName = (examClass) => {
    if (examClass.lecturerName) return examClass.lecturerName;

    const lecturer = lecturers.find((l) => l.id === examClass.lecturerId);

    if (!lecturer) return examClass.lecturerId || '-';

    return getLecturerDisplayName(lecturer);
  };

  // =========================
  // MASTER CLASSES HANDLERS
  // =========================
  const handleAddMasterClass = async (e) => {
    e.preventDefault();
    setError('');
    setMessage('');
    setLoading(true);

    try {
      if (editingMasterClass) {
        await api.put(`/Classes/${editingMasterClass.id}`, {
          id: editingMasterClass.id,
          classCode: newClassCode.trim()
        });
        setMessage('Class updated successfully!');
        setEditingMasterClass(null);
      } else {
        await api.post('/Classes', {
          classCode: newClassCode.trim()
        });
        setMessage('Class created successfully!');
      }
      setNewClassCode('');
      fetchMasterClasses();
    } catch (err) {
      setError(
        err.response?.data?.message ||
        err.response?.data?.detail ||
        `Failed to ${editingMasterClass ? 'update' : 'create'} class.`
      );
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
      'Are you sure you want to delete this class? This will fail if there are active exam classes for it.',
      async () => {
        closeConfirm();
        setError('');
        setMessage('');
        try {
          await api.delete(`/Classes/${id}`);
          setMessage('Class deleted successfully!');
          fetchMasterClasses();
        } catch (err) {
          setError(
            err.response?.data?.message ||
            err.response?.data?.detail ||
            'Failed to delete class.'
          );
        }
      }
    );
  };


  const totalCriteriaWeight = criteria.reduce(
    (sum, c) => sum + (Number(c.weight) || 0),
    0
  );

  return (
    <div className="dashboard-container fade-in">
      <div className="page-header">
        <div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 700 }}>
            Academic Staff Portal
          </h1>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.95rem' }}>
            Define courses, grading rubrics, exam classes, and submission ZIP uploads.
          </p>
        </div>

        <div
          className="glass-panel nav-links"
          style={{ padding: '0.4rem', borderRadius: '10px' }}
        >
          <button
            onClick={() => {
              setActiveTab('subjects');
              setError('');
              setMessage('');
            }}
            className={`btn btn-sm ${
              activeTab === 'subjects' ? 'btn-primary' : 'btn-secondary'
            }`}
            style={{ border: 'none' }}
          >
            <BookOpen size={16} /> Subjects
          </button>

          <button
            onClick={() => {
              setActiveTab('rubrics');
              setError('');
              setMessage('');
            }}
            className={`btn btn-sm ${
              activeTab === 'rubrics' ? 'btn-primary' : 'btn-secondary'
            }`}
            style={{ border: 'none' }}
          >
            <Award size={16} /> Rubrics
          </button>

          <button
            onClick={() => {
              setActiveTab('masterClasses');
              setError('');
              setMessage('');
            }}
            className={`btn btn-sm ${
              activeTab === 'masterClasses' ? 'btn-primary' : 'btn-secondary'
            }`}
            style={{ border: 'none' }}
          >
            <Layers size={16} /> Classes
          </button>


          <button
            onClick={() => {
              setActiveTab('classes');
              setError('');
              setMessage('');
            }}
            className={`btn btn-sm ${
              activeTab === 'classes' ? 'btn-primary' : 'btn-secondary'
            }`}
            style={{ border: 'none' }}
          >
            <ClipboardCheck size={16} /> Exam Classes
          </button>

          <button
            onClick={() => {
              setActiveTab('analytics');
              setError('');
              setMessage('');
            }}
            className={`btn btn-sm ${
              activeTab === 'analytics' ? 'btn-primary' : 'btn-secondary'
            }`}
            style={{ border: 'none' }}
          >
            <BarChart2 size={16} /> Analytics
          </button>
        </div>
      </div>

      {error && (
        <div
          style={{
            padding: '0.75rem 1rem',
            background: 'rgba(239, 68, 68, 0.15)',
            border: '1px solid rgba(239, 68, 68, 0.3)',
            borderRadius: '8px',
            color: 'var(--color-danger)',
            fontSize: '0.9rem',
            marginBottom: '1.5rem'
          }}
        >
          {error}
        </div>
      )}

      {message && (
        <div
          style={{
            padding: '0.75rem 1rem',
            background: 'rgba(16, 185, 129, 0.15)',
            border: '1px solid rgba(16, 185, 129, 0.3)',
            borderRadius: '8px',
            color: 'var(--color-success)',
            fontSize: '0.9rem',
            marginBottom: '1.5rem'
          }}
        >
          {message}
        </div>
      )}

      {/* Tab: Subjects */}
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
                    <th style={{ width: '100px', textAlign: 'center' }}>
                      Actions
                    </th>
                  </tr>
                </thead>

                <tbody>
                  {subjects.length === 0 ? (
                    <tr>
                      <td
                        colSpan="3"
                        style={{
                          textAlign: 'center',
                          color: 'var(--text-muted)'
                        }}
                      >
                        No subjects defined yet.
                      </td>
                    </tr>
                  ) : (
                    subjects.map((s) => (
                      <tr key={s.id}>
                        <td
                          style={{
                            fontWeight: 600,
                            color: 'var(--color-primary)'
                          }}
                        >
                          {s.subjectCode}
                        </td>
                        <td>{s.subjectName}</td>
                        <td style={{ textAlign: 'center' }}>
                          <div
                            style={{
                              display: 'flex',
                              gap: '0.25rem',
                              justifyContent: 'center'
                            }}
                          >
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

          <div className="glass-panel">
            <div className="card-header">
              {editingSubject ? 'Edit Subject' : 'Add New Subject'}
            </div>

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
                <button
                  type="submit"
                  disabled={loading}
                  className="btn btn-primary"
                  style={{ flex: 1 }}
                >
                  <Plus size={18} />{' '}
                  {loading
                    ? 'Saving...'
                    : editingSubject
                    ? 'Update Subject'
                    : 'Create Subject'}
                </button>

                {editingSubject && (
                  <button
                    type="button"
                    onClick={handleCancelEditSubject}
                    className="btn btn-secondary"
                  >
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
          <div className="glass-panel">
            <div
              className="card-header"
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center'
              }}
            >
              <span>Barem Điểm (Rubric Viewer)</span>

              <select
                className="form-control"
                style={{
                  width: '180px',
                  padding: '0.4rem 0.8rem',
                  fontSize: '0.9rem'
                }}
                value={selectedSubId}
                onChange={handleSubjectChange}
              >
                {subjects.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.subjectCode}
                  </option>
                ))}
              </select>
            </div>

            <div className="card-body">
              {currentRubric ? (
                <div>
                  <div
                    style={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                      marginBottom: '0.5rem'
                    }}
                  >
                    <h3
                      style={{
                        fontSize: '1.25rem',
                        fontWeight: 600,
                        color: 'var(--color-primary)',
                        margin: 0
                      }}
                    >
                      {currentRubric.name}
                    </h3>

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

                  <p
                    style={{
                      color: 'var(--text-muted)',
                      fontSize: '0.9rem',
                      marginBottom: '1.25rem'
                    }}
                  >
                    Tổng trọng số: {currentRubric.totalWeight}%
                  </p>

                  <div
                    style={{
                      display: 'flex',
                      flexDirection: 'column',
                      gap: '1rem'
                    }}
                  >
                    {currentRubric.criteria.map((c) => (
                      <div
                        key={c.id}
                        style={{
                          background: 'rgba(255,255,255,0.03)',
                          padding: '1rem',
                          border: '1px solid rgba(255,255,255,0.05)',
                          borderRadius: '8px'
                        }}
                      >
                        <div
                          style={{
                            display: 'flex',
                            justifyContent: 'space-between',
                            marginBottom: '0.25rem'
                          }}
                        >
                          <span style={{ fontWeight: 600 }}>
                            {c.criteriaName}
                          </span>
                          <span
                            style={{
                              color: 'var(--color-primary)',
                              fontWeight: 500
                            }}
                          >
                            {c.weight}% (Max: {c.maxPoints}đ)
                          </span>
                        </div>

                        <p
                          style={{
                            fontSize: '0.875rem',
                            color: 'var(--text-muted)'
                          }}
                        >
                          {c.description}
                        </p>
                      </div>
                    ))}
                  </div>
                </div>
              ) : (
                <div
                  style={{
                    textAlign: 'center',
                    padding: '3rem 1rem',
                    color: 'var(--text-muted)'
                  }}
                >
                  <Award
                    size={48}
                    style={{
                      strokeWidth: 1,
                      marginBottom: '1rem',
                      color: 'var(--text-muted)'
                    }}
                  />
                  <p>No rubric configured for this subject yet.</p>
                  <p style={{ fontSize: '0.85rem', marginTop: '0.5rem' }}>
                    Configure one in the right panel.
                  </p>
                </div>
              )}
            </div>
          </div>

          <div className="glass-panel">
            <div className="card-header">
              {editingRubricId
                ? 'Edit Rubric Criteria'
                : 'Configure Rubric Criteria'}
            </div>

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

              <div
                style={{
                  marginBottom: '1rem',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center'
                }}
              >
                <span
                  style={{
                    fontSize: '0.9rem',
                    fontWeight: 600,
                    color: 'var(--text-muted)'
                  }}
                >
                  Criteria Details
                </span>

                <button
                  type="button"
                  onClick={addCriteriaRow}
                  className="btn btn-secondary btn-sm"
                >
                  <Plus size={14} /> Add Row
                </button>
              </div>

              <div
                style={{
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '0.75rem',
                  marginBottom: '1.5rem',
                  maxHeight: '350px',
                  overflowY: 'auto'
                }}
              >
                {criteria.map((c, index) => (
                  <div
                    key={index}
                    style={{
                      display: 'flex',
                      gap: '0.5rem',
                      alignItems: 'flex-start',
                      background: 'rgba(255,255,255,0.02)',
                      padding: '0.75rem',
                      borderRadius: '8px'
                    }}
                  >
                    <div
                      style={{
                        flex: 1,
                        display: 'flex',
                        flexDirection: 'column',
                        gap: '0.4rem'
                      }}
                    >
                      <input
                        type="text"
                        required
                        placeholder="Criteria Name"
                        className="form-control"
                        style={{
                          padding: '0.4rem 0.8rem',
                          fontSize: '0.9rem'
                        }}
                        value={c.criteriaName}
                        onChange={(e) =>
                          updateCriteriaCell(
                            index,
                            'criteriaName',
                            e.target.value
                          )
                        }
                      />

                      <input
                        type="text"
                        placeholder="Description of criteria"
                        className="form-control"
                        style={{
                          padding: '0.4rem 0.8rem',
                          fontSize: '0.9rem'
                        }}
                        value={c.description}
                        onChange={(e) =>
                          updateCriteriaCell(
                            index,
                            'description',
                            e.target.value
                          )
                        }
                      />
                    </div>

                    <div style={{ width: '80px' }}>
                      <label
                        style={{
                          fontSize: '0.75rem',
                          color: 'var(--text-muted)'
                        }}
                      >
                        Max Pts
                      </label>
                      <input
                        type="number"
                        required
                        min="1"
                        className="form-control"
                        style={{
                          padding: '0.4rem 0.8rem',
                          fontSize: '0.9rem'
                        }}
                        value={c.maxPoints}
                        onChange={(e) =>
                          updateCriteriaCell(index, 'maxPoints', e.target.value)
                        }
                      />
                    </div>

                    <div style={{ width: '80px' }}>
                      <label
                        style={{
                          fontSize: '0.75rem',
                          color: 'var(--text-muted)'
                        }}
                      >
                        Weight (%)
                      </label>
                      <input
                        type="number"
                        required
                        min="1"
                        max="100"
                        className="form-control"
                        style={{
                          padding: '0.4rem 0.8rem',
                          fontSize: '0.9rem'
                        }}
                        value={c.weight}
                        onChange={(e) =>
                          updateCriteriaCell(index, 'weight', e.target.value)
                        }
                      />
                    </div>

                    {criteria.length > 1 && (
                      <button
                        type="button"
                        onClick={() => removeCriteriaRow(index)}
                        className="btn btn-danger btn-sm"
                        style={{
                          padding: '0.5rem',
                          marginTop: '1.25rem'
                        }}
                      >
                        <Trash2 size={14} />
                      </button>
                    )}
                  </div>
                ))}
              </div>

              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  background: 'rgba(255,255,255,0.03)',
                  padding: '0.75rem 1rem',
                  borderRadius: '8px',
                  marginBottom: '1.5rem'
                }}
              >
                <span style={{ fontSize: '0.9rem' }}>Tổng trọng số:</span>

                <span
                  style={{
                    fontWeight: 700,
                    color:
                      totalCriteriaWeight === 100
                        ? 'var(--color-success)'
                        : 'var(--color-danger)'
                  }}
                >
                  {totalCriteriaWeight}% / 100%
                </span>
              </div>

              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <button
                  type="submit"
                  disabled={loading}
                  className="btn btn-primary"
                  style={{ flex: 1 }}
                >
                  <ClipboardCheck size={18} />{' '}
                  {loading
                    ? 'Saving...'
                    : editingRubricId
                    ? 'Update Rubric'
                    : 'Save Rubric'}
                </button>

                {editingRubricId && (
                  <button
                    type="button"
                    onClick={handleCancelEditRubric}
                    className="btn btn-secondary"
                  >
                    Cancel
                  </button>
                )}
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Tab: Classes (Master Classes CRUD) */}
      {activeTab === 'masterClasses' && (
        <div className="grid-cols-2">
          <div className="glass-panel" style={{ overflow: 'hidden' }}>
            <div className="card-header">All Classes</div>
            <div style={{ maxHeight: '560px', overflow: 'auto' }}>
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
                        No classes found. Add one on the right.
                      </td>
                    </tr>
                  ) : (
                    masterClasses.map((c) => (
                      <tr key={c.id}>
                        <td style={{ fontWeight: 600 }}>{c.classCode}</td>
                        <td>
                          <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'center' }}>
                            <button
                              onClick={() => handleEditMasterClass(c)}
                              className="btn btn-secondary btn-sm"
                              style={{ padding: '0.3rem 0.5rem' }}
                              title="Edit"
                            >
                              <Edit3 size={14} />
                            </button>
                            <button
                              onClick={() => handleDeleteMasterClass(c.id)}
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

          <div className="glass-panel">
            <div className="card-header">
              {editingMasterClass ? 'Edit Class' : 'Create Class'}
            </div>
            <form onSubmit={handleAddMasterClass} className="card-body">
              <div className="form-group">
                <label className="form-label">Class Code</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. SE1801"
                  className="form-control"
                  value={newClassCode}
                  onChange={(e) => setNewClassCode(e.target.value)}
                />
              </div>

              <div style={{ display: 'flex', gap: '0.5rem', marginTop: '1.5rem' }}>
                <button type="submit" disabled={loading} className="btn btn-primary" style={{ flex: 1 }}>
                  {editingMasterClass ? 'Save Changes' : 'Create Class'}
                </button>
                {editingMasterClass && (
                  <button
                    type="button"
                    onClick={handleCancelEditMasterClass}
                    className="btn btn-secondary"
                    style={{ flex: 0.5 }}
                  >
                    Cancel
                  </button>
                )}
              </div>
            </form>
          </div>
        </div>
      )}


      {/* Tab: Exam Classes */}
      {activeTab === 'classes' && (
        <div className="grid-cols-2">
          <div className="glass-panel" style={{ overflow: 'hidden' }}>
            <div className="card-header">Exam Classes</div>

            <div style={{ maxHeight: '560px', overflow: 'auto' }}>
              <table className="custom-table">
                <thead>
                  <tr>
                    <th>Class</th>
                    <th>Subject</th>
                    <th>Semester</th>
                    <th>Lecturer</th>
                    <th>Status</th>
                    <th>Submissions</th>
                    <th style={{ textAlign: 'center' }}>Export</th>
                  </tr>
                </thead>

                <tbody>
                  {classes.length === 0 ? (
                    <tr>
                      <td
                        colSpan="7"
                        style={{
                          textAlign: 'center',
                          color: 'var(--text-muted)'
                        }}
                      >
                        No exam classes created yet.
                      </td>
                    </tr>
                  ) : (
                    classes.map((c) => (
                      <tr key={c.id}>
                        <td
                          style={{
                            fontWeight: 600,
                            color: 'var(--color-primary)'
                          }}
                        >
                          {c.classCode}
                        </td>

                        <td>{getClassSubjectName(c)}</td>
                        <td>{c.semester}</td>
                        <td>{getClassLecturerName(c)}</td>
                        <td>
                          <span
                            style={{
                              padding: '0.15rem 0.5rem',
                              borderRadius: '999px',
                              fontSize: '0.78rem',
                              fontWeight: 600,
                              background:
                                c.status === 'Completed'
                                  ? 'rgba(34,197,94,0.12)'
                                  : c.status === 'Grading'
                                  ? 'rgba(234,179,8,0.12)'
                                  : 'rgba(148,163,184,0.12)',
                              color:
                                c.status === 'Completed'
                                  ? 'var(--color-success)'
                                  : c.status === 'Grading'
                                  ? '#b45309'
                                  : 'var(--text-muted)'
                            }}
                          >
                            {c.status}
                          </span>
                        </td>
                        <td style={{ textAlign: 'center' }}>
                          {c.submissionCount ?? '—'}
                        </td>
                        <td style={{ textAlign: 'center' }}>
                          <button
                            onClick={() => handleExportExcel(c.id)}
                            className="btn btn-secondary btn-sm"
                            style={{ padding: '0.3rem 0.5rem', gap: '0.25rem' }}
                            title="Export Excel"
                          >
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
            <div className="card-header">Create Exam Class</div>

            <form onSubmit={handleSubmitClass} className="card-body">
              <div className="form-group">
                <label className="form-label">Class Code</label>
                <select
                  className="form-control"
                  value={classCode}
                  onChange={(e) => setClassCode(e.target.value)}
                  required
                >
                  <option value="">Select class</option>
                  {masterClasses.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.classCode}
                    </option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">Subject</label>
                <select
                  className="form-control"
                  value={classSubjectId}
                  onChange={(e) => setClassSubjectId(e.target.value)}
                  required
                >
                  <option value="">Select subject</option>
                  {subjects.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.subjectCode} - {s.subjectName}
                    </option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">Semester</label>
                <input
                  type="text"
                  required
                  placeholder="SU26"
                  className="form-control"
                  value={semester}
                  onChange={(e) => setSemester(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label className="form-label">Lecturer</label>
                <select
                  className="form-control"
                  value={lecturerId}
                  onChange={(e) => setLecturerId(e.target.value)}
                  required
                >
                  <option value="">Select lecturer</option>
                  {lecturers.map((l) => (
                    <option key={l.id} value={l.id}>
                      {getLecturerDisplayName(l)}
                    </option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">Submissions ZIP (optional)</label>
                <div
                  style={{
                    border: '2px dashed var(--border-color)',
                    borderRadius: '0.5rem',
                    padding: '1rem',
                    textAlign: 'center',
                    cursor: 'pointer',
                    background: zipFile ? 'rgba(234,88,12,0.05)' : 'transparent',
                    transition: 'all 0.2s'
                  }}
                  onClick={() => document.getElementById('zip-file-input').click()}
                >
                  <Upload size={20} style={{ color: 'var(--text-muted)', marginBottom: '0.25rem' }} />
                  {zipFile ? (
                    <div>
                      <div style={{ fontWeight: 600, fontSize: '0.9rem' }}>{zipFile.name}</div>
                      <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                        {(zipFile.size / 1024 / 1024).toFixed(2)} MB
                      </div>
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          setZipFile(null);
                        }}
                        style={{
                          marginTop: '0.35rem',
                          fontSize: '0.78rem',
                          color: 'var(--color-danger)',
                          background: 'none',
                          border: 'none',
                          cursor: 'pointer',
                          textDecoration: 'underline'
                        }}
                      >
                        Remove file
                      </button>
                    </div>
                  ) : (
                    <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                      Click to select a .zip file
                    </div>
                  )}
                  <input
                    id="zip-file-input"
                    type="file"
                    accept=".zip"
                    style={{ display: 'none' }}
                    onChange={(e) => {
                      const f = e.target.files?.[0];
                      if (f && f.name.toLowerCase().endsWith('.zip')) {
                        setZipFile(f);
                      } else if (f) {
                        setError('Only .zip files are allowed.');
                      }
                      e.target.value = '';
                    }}
                  />
                </div>
              </div>

              <button
                type="submit"
                disabled={loading}
                className="btn btn-primary"
                style={{ width: '100%', marginTop: '0.5rem' }}
              >
                <Plus size={18} />{' '}
                {loading ? 'Creating...' : 'Create Exam Class'}
              </button>
            </form>
          </div>
        </div>
      )}

      {/* Tab: Analytics */}
      {activeTab === 'analytics' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
          {/* Class selector */}
          <div className="glass-panel card-body" style={{ display: 'flex', gap: '1rem', alignItems: 'center', flexWrap: 'wrap' }}>
            <BarChart2 size={20} style={{ color: 'var(--color-primary)', flexShrink: 0 }} />
            <span style={{ fontWeight: 600, fontSize: '0.95rem' }}>Grading Analytics Dashboard</span>
            <select
              className="form-control"
              style={{ maxWidth: '320px', flex: 1 }}
              value={analyticsClassId}
              onChange={(e) => {
                setAnalyticsClassId(e.target.value);
                fetchAnalytics(e.target.value);
              }}
            >
              <option value="">— Select an Exam Class —</option>
              {classes.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.classCode} · {c.subjectCode} · {c.semester}
                </option>
              ))}
            </select>
            {analyticsClassId && (
              <button
                className="btn btn-secondary btn-sm"
                onClick={() => fetchAnalytics(analyticsClassId)}
                disabled={analyticsLoading}
              >
                {analyticsLoading ? 'Loading...' : 'Refresh'}
              </button>
            )}
          </div>

          {analyticsLoading && (
            <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
              <div style={{ fontSize: '0.95rem' }}>Loading analytics…</div>
            </div>
          )}

          {!analyticsLoading && !analyticsData && analyticsClassId && (
            <div className="glass-panel card-body" style={{ textAlign: 'center', color: 'var(--text-muted)', padding: '3rem' }}>
              No data found for this exam class.
            </div>
          )}

          {!analyticsLoading && !analyticsClassId && (
            <div className="glass-panel card-body" style={{ textAlign: 'center', color: 'var(--text-muted)', padding: '3rem' }}>
              <BarChart2 size={48} style={{ strokeWidth: 1, marginBottom: '1rem', opacity: 0.4 }} />
              <p>Select an exam class above to view its analytics.</p>
            </div>
          )}

          {analyticsData && !analyticsLoading && (() => {
            const d = analyticsData;
            const maxCount = Math.max(...d.scoreDistribution.map(b => b.count), 1);

            return (
              <>
                {/* Stat Cards */}
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))', gap: '1rem' }}>
                  {[
                    { label: 'Avg Score', value: d.averageScore?.toFixed(2) ?? '—', color: 'var(--color-primary)' },
                    { label: 'Min Score', value: d.minScore?.toFixed(2) ?? '—', color: '#f59e0b' },
                    { label: 'Max Score', value: d.maxScore?.toFixed(2) ?? '—', color: 'var(--color-success)' },
                    { label: 'Total Graded', value: `${d.totalGraded} / ${d.totalSubmissions}`, color: 'var(--text-secondary)' },
                    { label: 'Pass Rate', value: `${d.passRate?.toFixed(1)}%`, color: 'var(--color-success)' },
                    { label: 'Fail Rate', value: `${d.failRate?.toFixed(1)}%`, color: 'var(--color-danger)' },
                    { label: 'Excellent (≥8)', value: `${d.excellentRate?.toFixed(1)}%`, color: '#a78bfa' },
                  ].map((card) => (
                    <div key={card.label} className="glass-panel" style={{ padding: '1rem', textAlign: 'center' }}>
                      <div style={{ fontSize: '1.5rem', fontWeight: 700, color: card.color }}>{card.value}</div>
                      <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>{card.label}</div>
                    </div>
                  ))}
                </div>

                {/* Score Distribution Bar Chart */}
                <div className="glass-panel" style={{ padding: '1.5rem' }}>
                  <div className="card-header" style={{ padding: 0, marginBottom: '1.25rem', background: 'none', borderBottom: 'none' }}>
                    Phổ Điểm (Score Distribution)
                  </div>
                  {d.totalGraded === 0 ? (
                    <p style={{ color: 'var(--text-muted)', textAlign: 'center', padding: '2rem' }}>
                      No graded submissions yet.
                    </p>
                  ) : (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                      {d.scoreDistribution.map((bucket) => {
                        const pct = maxCount > 0 ? (bucket.count / maxCount) * 100 : 0;
                        const scoreStart = parseInt(bucket.label.split('-')[0]);
                        const barColor = scoreStart >= 8
                          ? 'var(--color-success)'
                          : scoreStart >= 5
                            ? '#f59e0b'
                            : 'var(--color-danger)';
                        return (
                          <div key={bucket.label} style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                            <span style={{ width: '36px', textAlign: 'right', fontSize: '0.8rem', color: 'var(--text-muted)', flexShrink: 0 }}>
                              {bucket.label}
                            </span>
                            <div style={{ flex: 1, background: 'rgba(255,255,255,0.05)', borderRadius: '4px', overflow: 'hidden', height: '22px' }}>
                              <div
                                style={{
                                  width: `${pct}%`,
                                  height: '100%',
                                  background: barColor,
                                  borderRadius: '4px',
                                  transition: 'width 0.5s ease',
                                  minWidth: bucket.count > 0 ? '4px' : '0'
                                }}
                              />
                            </div>
                            <span style={{ width: '28px', fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-secondary)' }}>
                              {bucket.count}
                            </span>
                          </div>
                        );
                      })}
                      <div style={{ display: 'flex', gap: '1.5rem', marginTop: '0.75rem', paddingTop: '0.75rem', borderTop: '1px solid rgba(255,255,255,0.07)', flexWrap: 'wrap' }}>
                        {[
                          { color: 'var(--color-danger)', label: 'Fail (0–4)' },
                          { color: '#f59e0b', label: 'Pass (5–7)' },
                          { color: 'var(--color-success)', label: 'Excellent (8–10)' },
                        ].map((leg) => (
                          <div key={leg.label} style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                            <div style={{ width: '12px', height: '12px', borderRadius: '3px', background: leg.color }} />
                            {leg.label}
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>

                {/* Criteria Difficulty Analysis */}
                {d.criteriaAnalysis && d.criteriaAnalysis.length > 0 && (
                  <div className="glass-panel" style={{ overflow: 'hidden' }}>
                    <div className="card-header">
                      Phân Tích Độ Khó Tiêu Chí (Criteria Difficulty Analysis)
                      <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginLeft: '0.5rem', fontWeight: 400 }}>
                        — sorted by worst performing first
                      </span>
                    </div>
                    <div style={{ overflowX: 'auto' }}>
                      <table className="custom-table">
                        <thead>
                          <tr>
                            <th>Criteria</th>
                            <th>Max Points</th>
                            <th>Weight</th>
                            <th>Avg Score</th>
                            <th>Avg %</th>
                            <th>Students Missing (&lt;70%)</th>
                            <th>Miss Rate</th>
                          </tr>
                        </thead>
                        <tbody>
                          {d.criteriaAnalysis.map((c, i) => {
                            const avgPct = c.averageScorePercent ?? 0;
                            const barColor = avgPct >= 70
                              ? 'var(--color-success)'
                              : avgPct >= 50
                                ? '#f59e0b'
                                : 'var(--color-danger)';
                            return (
                              <tr key={i}>
                                <td style={{ fontWeight: 600 }}>{c.criteriaName}</td>
                                <td style={{ textAlign: 'center' }}>{c.maxPoints}</td>
                                <td style={{ textAlign: 'center' }}>{c.weight}%</td>
                                <td style={{ textAlign: 'center', fontWeight: 600, color: barColor }}>
                                  {c.averageScore?.toFixed(2)}
                                </td>
                                <td>
                                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                                    <div style={{ flex: 1, height: '8px', background: 'rgba(255,255,255,0.05)', borderRadius: '4px', overflow: 'hidden' }}>
                                      <div style={{ width: `${Math.min(avgPct, 100)}%`, height: '100%', background: barColor, transition: 'width 0.4s ease', borderRadius: '4px' }} />
                                    </div>
                                    <span style={{ fontSize: '0.8rem', minWidth: '42px', color: barColor, fontWeight: 600 }}>
                                      {avgPct.toFixed(1)}%
                                    </span>
                                  </div>
                                </td>
                                <td style={{ textAlign: 'center', color: 'var(--color-danger)', fontWeight: 600 }}>
                                  {c.studentsMissingPoints}
                                </td>
                                <td style={{ textAlign: 'center' }}>
                                  <span style={{
                                    padding: '0.15rem 0.5rem',
                                    borderRadius: '999px',
                                    fontSize: '0.78rem',
                                    fontWeight: 600,
                                    background: c.missRate > 50 ? 'rgba(239,68,68,0.12)' : c.missRate > 25 ? 'rgba(245,158,11,0.12)' : 'rgba(34,197,94,0.12)',
                                    color: c.missRate > 50 ? 'var(--color-danger)' : c.missRate > 25 ? '#b45309' : 'var(--color-success)'
                                  }}>
                                    {c.missRate?.toFixed(1)}%
                                  </span>
                                </td>
                              </tr>
                            );
                          })}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}
              </>
            );
          })()}
        </div>
      )}

      {/* Confirm Delete Modal */}
      {confirmModal.show && (
        <div className="confirm-overlay" onClick={closeConfirm}>
          <div
            className="glass-panel confirm-modal"
            onClick={(e) => e.stopPropagation()}
          >
            <div style={{ textAlign: 'center', marginBottom: '1.25rem' }}>
              <div
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  width: '48px',
                  height: '48px',
                  borderRadius: '50%',
                  background: 'rgba(239, 68, 68, 0.12)',
                  marginBottom: '0.75rem'
                }}
              >
                <AlertTriangle
                  size={24}
                  style={{ color: 'var(--color-danger)' }}
                />
              </div>

              <h3
                style={{
                  fontSize: '1.15rem',
                  fontWeight: 700,
                  marginBottom: '0.5rem'
                }}
              >
                {confirmModal.title}
              </h3>

              <p
                style={{
                  color: 'var(--text-muted)',
                  fontSize: '0.9rem'
                }}
              >
                {confirmModal.message}
              </p>
            </div>

            <div style={{ display: 'flex', gap: '0.75rem' }}>
              <button
                className="btn btn-secondary"
                style={{ flex: 1 }}
                onClick={closeConfirm}
              >
                Cancel
              </button>

              <button
                className="btn btn-danger"
                style={{ flex: 1 }}
                onClick={confirmModal.onConfirm}
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}