import React, { useState, useEffect } from 'react';
import { ChevronLeft, Save, CheckCircle, FileText, HelpCircle } from 'lucide-react';
import api from '../api';

export default function GradingView({ submissionId, examClassId, onBack }) {
  const [submission, setSubmission] = useState(null);
  const [rubric, setRubric] = useState(null);
  
  // Grade Form state
  const [scores, setScores] = useState({}); // { criteriaId: score }
  const [feedbacks, setFeedbacks] = useState({}); // { criteriaId: comment }
  const [generalFeedback, setGeneralFeedback] = useState('');
  
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  // Document Content state
  const [fileContent, setFileContent] = useState('');
  const [contentLoading, setContentLoading] = useState(false);
  const [contentError, setContentError] = useState('');

  useEffect(() => {
    fetchSubmissionData();
  }, [submissionId]);

  const fetchDocumentContent = async (subId) => {
    setContentLoading(true);
    setContentError('');
    try {
      const contentRes = await api.get(`/Submissions/${subId}/content`);
      setFileContent(contentRes.data.content);
    } catch (err) {
      console.error(err);
      setContentError(err.response?.data?.message || 'Failed to load document content.');
    } finally {
      setContentLoading(false);
    }
  };

  const fetchSubmissionData = async () => {
    setLoading(true);
    setError('');
    try {
      // 1. Get submission and existing grade
      const subRes = await api.get(`/Grades/submission/${submissionId}`);
      setSubmission(subRes.data);

      // Fetch document content via gRPC
      fetchDocumentContent(subRes.data.id);
      
      // Load existing grades if present
      if (subRes.data.grade) {
        setGeneralFeedback(subRes.data.grade.generalFeedback || '');
        const sMap = {};
        const fMap = {};
        subRes.data.grade.details.forEach(d => {
          sMap[d.rubricCriteriaId] = d.score;
          fMap[d.rubricCriteriaId] = d.feedback || '';
        });
        setScores(sMap);
        setFeedbacks(fMap);
      }

      // 2. Get Rubric for class subject
      const classRes = await api.get('/ExamClasses');
      const currentClass = classRes.data.find(c => c.id === examClassId);
      
      if (currentClass) {
        try {
          const rubricRes = await api.get(`/Rubrics/subject/${currentClass.subjectId}`);
          setRubric(rubricRes.data);

          // Prepopulate empty scores if no existing grade
          if (!subRes.data.grade && rubricRes.data.criteria) {
            const sMap = {};
            const fMap = {};
            rubricRes.data.criteria.forEach(c => {
              sMap[c.id] = 0;
              fMap[c.id] = '';
            });
            setScores(sMap);
            setFeedbacks(fMap);
          }
        } catch (rubricErr) {
          console.warn('No rubric found for this subject:', rubricErr);
          setRubric(null);
        }
      }
    } catch (err) {
      console.error(err);
      setError('Failed to load grading assets.');
    } finally {
      setLoading(false);
    }
  };

  // Dynamic Total Score Calculation
  const calculateTotalScore = () => {
    if (!rubric) return 0;
    
    let totalScore = 0;
    const totalWeight = rubric.criteria.reduce((sum, c) => sum + c.weight, 0);
    const isPercentage = Math.abs(totalWeight - 100) < 0.1;

    rubric.criteria.forEach(c => {
      const score = scores[c.id] || 0;
      if (isPercentage) {
        totalScore += score * (c.weight / 100);
      } else {
        totalScore += score * c.weight;
      }
    });

    return Math.round(totalScore * 100) / 100;
  };

  const handleScoreChange = (criteriaId, score) => {
    setScores({ ...scores, [criteriaId]: score });
  };

  const handleFeedbackChange = (criteriaId, comment) => {
    setFeedbacks({ ...feedbacks, [criteriaId]: comment });
  };

  const submitGrade = async (isDraft) => {
    setSaving(true);
    setError('');

    const details = Object.keys(scores).map(cid => ({
      rubricCriteriaId: parseInt(cid),
      score: scores[cid],
      feedback: feedbacks[cid] || ''
    }));

    try {
      await api.post('/Grades/submission', {
        submissionId: parseInt(submissionId),
        generalFeedback,
        isDraft,
        details
      });
      alert(isDraft ? 'Draft saved successfully!' : 'Grade published successfully!');
      onBack();
    } catch (err) {
      console.error(err);
      setError(err.response?.data?.detail || 'Failed to submit grade.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '80vh', color: 'var(--text-muted)' }}>
        Loading student submissions and barem rubric...
      </div>
    );
  }

  if (!submission) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-muted)' }}>
        {error || 'Submission data unavailable.'}
        <br />
        <button onClick={onBack} className="btn btn-secondary btn-sm" style={{ marginTop: '1rem' }}>
          Back to Dashboard
        </button>
      </div>
    );
  }

  return (
    <div className="split-screen fade-in">
      {/* Left panel: Document Viewer */}
      <div className="left-panel">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <button onClick={onBack} className="btn btn-secondary btn-sm" style={{ padding: '0.4rem 0.6rem' }}>
            <ChevronLeft size={16} /> Dashboard
          </button>
          <div style={{ textAlign: 'right' }}>
            <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Student Submission</span>
            <h3 style={{ fontSize: '1.1rem', fontWeight: 600 }}>{submission.studentName} ({submission.studentId})</h3>
          </div>
        </div>

        {/* Document Viewer (Loaded dynamically via gRPC) */}
        <div className="document-preview-card" style={{ display: 'flex', flexDirection: 'column', height: 'calc(100% - 100px)', overflowY: 'auto' }}>
          <div style={{ borderBottom: '1px solid #e2e8f0', paddingBottom: '0.5rem', marginBottom: '1.5rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: '0.85rem', color: '#64748b' }}>
            <span>File: {submission.filePath ? submission.filePath.split('\\').pop().split('/').pop() : ''}</span>
            <span style={{ textTransform: 'uppercase', background: '#e2e8f0', padding: '0.15rem 0.4rem', borderRadius: '4px', fontWeight: 'bold' }}>{submission.fileType}</span>
          </div>

          {contentLoading ? (
            <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', flex: 1, color: 'var(--text-muted)' }}>
              Loading document content via gRPC...
            </div>
          ) : contentError ? (
            <div style={{ color: 'var(--color-danger)', padding: '1rem', background: 'rgba(239, 68, 68, 0.05)', borderRadius: '6px' }}>
              {contentError}
            </div>
          ) : (
            <div style={{ whiteSpace: 'pre-wrap', fontFamily: 'monospace', lineHeight: '1.6', fontSize: '0.9rem', flex: 1, background: '#ffffff', color: '#0f172a', border: '1px solid #e2e8f0', padding: '1.25rem', borderRadius: '8px', overflowX: 'auto', boxShadow: 'inset 0 1px 2px rgba(0,0,0,0.03)' }}>
              {fileContent || 'Document is empty.'}
            </div>
          )}
        </div>
      </div>

      {/* Right panel: Rubric Form */}
      <div className="right-panel">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <div>
            <h3 style={{ fontSize: '1.2rem', fontWeight: 700 }}>Grading Rubric</h3>
            <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{rubric ? rubric.name : 'Chưa thiết lập Rubric'}</span>
          </div>
          {/* Dynamic Score Display */}
          {rubric && (
            <div style={{ background: 'rgba(242, 113, 33, 0.1)', border: '1px solid rgba(242, 113, 33, 0.2)', padding: '0.5rem 1rem', borderRadius: '10px', textAlign: 'center' }}>
              <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'block', textTransform: 'uppercase', fontWeight: 600 }}>Total Score</span>
              <span style={{ fontSize: '1.5rem', fontWeight: 800, color: 'var(--color-primary)' }}>{calculateTotalScore()} / 10</span>
            </div>
          )}
        </div>

        {error && (
          <div style={{ padding: '0.5rem 0.75rem', background: 'rgba(239, 68, 68, 0.15)', border: '1px solid rgba(239, 68, 68, 0.3)', borderRadius: '6px', color: 'var(--color-danger)', fontSize: '0.85rem', marginBottom: '1rem' }}>
            {error}
          </div>
        )}

        {!rubric ? (
          <div style={{ padding: '1.5rem', background: 'rgba(242, 113, 33, 0.05)', border: '1px solid rgba(242, 113, 33, 0.2)', borderRadius: '8px', color: 'var(--text-primary)' }}>
            <h4 style={{ fontWeight: 600, color: 'var(--color-primary)', marginBottom: '0.5rem' }}>Chưa có Barem / Rubric chấm điểm</h4>
            <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', lineHeight: '1.5' }}>
              Môn học này chưa được khởi tạo Thang điểm (Rubric). Hãy đăng nhập tài khoản <b>Academic Staff</b> để tạo Rubric trong mục <b>Barem / Rubrics</b>.
            </p>
          </div>
        ) : (
          <>
            {/* Criteria List */}
        <div style={{ flex: 1, overflowY: 'auto', paddingRight: '0.25rem' }}>
          {rubric.criteria.map(c => {
            const currentScore = scores[c.id] || 0;
            const maxPoints = Math.round(c.maxPoints);

            return (
              <div key={c.id} className="criteria-grading-card">
                <div className="criteria-header">
                  <div>
                    <h4 style={{ fontWeight: 600, fontSize: '0.95rem' }}>{c.criteriaName}</h4>
                    <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.1rem' }}>{c.description}</p>
                  </div>
                  <span style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--color-primary)' }}>{c.weight}% Weight</span>
                </div>

                {/* Score Button Selector */}
                <div className="score-selector">
                  {Array.from({ length: maxPoints + 1 }, (_, i) => (
                    <button
                      key={i}
                      type="button"
                      onClick={() => handleScoreChange(c.id, i)}
                      className={`score-btn ${currentScore === i ? 'selected' : ''}`}
                    >
                      {i}
                    </button>
                  ))}
                </div>

                {/* Criteria specific comment */}
                <input
                  type="text"
                  placeholder="Specific comment for this criteria..."
                  className="form-control"
                  style={{ marginTop: '0.75rem', fontSize: '0.85rem', padding: '0.4rem 0.6rem', borderRadius: '6px' }}
                  value={feedbacks[c.id] || ''}
                  onChange={(e) => handleFeedbackChange(c.id, e.target.value)}
                />
              </div>
            );
          })}

          {/* General Feedback */}
          <div style={{ marginTop: '1.5rem', marginBottom: '2rem' }}>
            <label className="form-label" style={{ fontWeight: 600 }}>General Feedback</label>
            <textarea
              rows="3"
              placeholder="Overall remarks for student..."
              className="form-control"
              style={{ fontSize: '0.9rem', resize: 'vertical' }}
              value={generalFeedback}
              onChange={(e) => setGeneralFeedback(e.target.value)}
            />
          </div>
        </div>

        {/* Action buttons */}
        <div style={{ borderTop: '1px solid var(--panel-border)', paddingTop: '1.25rem', display: 'flex', gap: '0.75rem' }}>
          <button onClick={() => submitGrade(true)} disabled={saving} className="btn btn-secondary" style={{ flex: 1 }}>
            <Save size={16} /> {saving ? 'Saving...' : 'Save Draft'}
          </button>
          <button onClick={() => submitGrade(false)} disabled={saving} className="btn btn-primary" style={{ flex: 1.2 }}>
            <CheckCircle size={16} /> {saving ? 'Submitting...' : 'Submit & Publish'}
          </button>
        </div>
          </>
        )}
      </div>
    </div>
  );
}
