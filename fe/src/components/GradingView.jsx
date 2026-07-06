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

  useEffect(() => {
    fetchSubmissionData();
  }, [submissionId]);

  const fetchSubmissionData = async () => {
    setLoading(true);
    setError('');
    try {
      // 1. Get submission and existing grade
      const subRes = await api.get(`/Grades/submission/${submissionId}`);
      setSubmission(subRes.data);
      
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
      // To get subject ID, we need subjectId, but let's query exam class
      const classRes = await api.get('/ExamClasses');
      const currentClass = classRes.data.find(c => c.id === examClassId);
      
      if (currentClass) {
        const rubricRes = await api.get(`/Rubrics/subject/${currentClass.subjectId}`);
        setRubric(rubricRes.data);

        // Prepopulate empty scores if no existing grade
        if (!subRes.data.grade) {
          const sMap = {};
          const fMap = {};
          rubricRes.data.criteria.forEach(c => {
            sMap[c.id] = 0;
            fMap[c.id] = '';
          });
          setScores(sMap);
          setFeedbacks(fMap);
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

  if (!submission || !rubric) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-muted)' }}>
        {error || 'Submission or Rubric data unavailable.'}
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

        {/* Word Document Mock View (Will load actual text content in Phase 3/4) */}
        <div className="document-preview-card">
          <div style={{ borderBottom: '1px solid #e2e8f0', paddingBottom: '0.5rem', marginBottom: '1.5rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: '0.85rem', color: '#64748b' }}>
            <span>File: {submission.filePath.split('/').pop()}</span>
            <span style={{ textTransform: 'uppercase', background: '#e2e8f0', padding: '0.15rem 0.4rem', borderRadius: '4px', fontWeight: 'bold' }}>{submission.fileType}</span>
          </div>

          <h1 style={{ textAlign: 'center', fontSize: '1.5rem', marginBottom: '2rem', fontWeight: 'bold' }}>
            PRACTICAL EXAM - WRITING ESSAY
          </h1>

          <p style={{ marginBottom: '1.25rem', textIndent: '2rem' }}>
            In recent years, the rapid advancement of technology has dramatically transformed the global business landscape. As enterprise systems become more integrated and complex, organizations are increasingly shifting from monolithic architectures to distributed cloud-native designs. This transition provides substantial benefits, including improved scalability, enhanced fault tolerance, and faster deployment cycles.
          </p>
          
          <p style={{ marginBottom: '1.25rem', textIndent: '2rem' }}>
            Furthermore, the introduction of asynchronous message brokers, such as Apache Kafka and Redis, has enabled seamless inter-service communication. Instead of relying on tightly coupled RESTful API calls, microservices can publish and subscribe to real-time events. This decoupled event-driven model ensures that service failures do not disrupt the entire workflow, thereby guaranteeing high availability and robust data integrity.
          </p>

          <p style={{ marginBottom: '1.25rem', textIndent: '2rem' }}>
            In conclusion, building distributed systems using modern frameworks like .NET Core and hosting them in containerized Docker environments is no longer a luxury, but a necessity for scaling modern enterprises. While managing distributed state and maintaining network reliability present challenges, the architectural advantages far outweigh the operational complexities.
          </p>

          <div style={{ marginTop: '4rem', fontSize: '0.85rem', color: '#94a3b8', fontStyle: 'italic', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <FileText size={16} />
            <span>Document Previewing Mode. In Phase 4, the gRPC Converter Service will extract and render custom content.</span>
          </div>
        </div>
      </div>

      {/* Right panel: Rubric Form */}
      <div className="right-panel">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <div>
            <h3 style={{ fontSize: '1.2rem', fontWeight: 700 }}>Grading Rubric</h3>
            <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{rubric.name}</span>
          </div>
          {/* Dynamic Score Display */}
          <div style={{ background: 'rgba(242, 113, 33, 0.1)', border: '1px solid rgba(242, 113, 33, 0.2)', padding: '0.5rem 1rem', borderRadius: '10px', textAlign: 'center' }}>
            <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'block', textTransform: 'uppercase', fontWeight: 600 }}>Total Score</span>
            <span style={{ fontSize: '1.5rem', fontWeight: 800, color: 'var(--color-primary)' }}>{calculateTotalScore()} / 10</span>
          </div>
        </div>

        {error && (
          <div style={{ padding: '0.5rem 0.75rem', background: 'rgba(239, 68, 68, 0.15)', border: '1px solid rgba(239, 68, 68, 0.3)', borderRadius: '6px', color: 'var(--color-danger)', fontSize: '0.85rem', marginBottom: '1rem' }}>
            {error}
          </div>
        )}

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
      </div>
    </div>
  );
}
