import { useState, useEffect } from 'react';
import axios from 'axios';
import './App.css';

const allWeekdays = ['Esmaspäev', 'Teisipäev', 'Kolmapäev', 'Neljapäev', 'Reede', 'Laupäev', 'Pühapäev'];
const allMealTypes = ['Hommikusöök', 'Lõuna', 'Õhtusöök'];

function App() {
  const [comparisonData, setComparisonData] = useState(null);

  const [people, setPeople] = useState(() => {
    const saved = localStorage.getItem('people');
    return saved ? Number(saved) : 2;
  });

  const [budget, setBudget] = useState(() => {
    const saved = localStorage.getItem('budget');
    return saved ? Number(saved) : 50;
  });

  const [selectedDays, setSelectedDays] = useState(() => {
    const saved = localStorage.getItem('selectedDays');
    return saved ? JSON.parse(saved) : [];
  });

  const [mealSelections, setMealSelections] = useState(() => {
    const saved = localStorage.getItem('mealSelections');
    return saved ? JSON.parse(saved) : {};
  });

  const [results, setResults] = useState(() => {
    const saved = localStorage.getItem('results');
    return saved ? JSON.parse(saved) : null;
  });

  const [shoppingList, setShoppingList] = useState(() => {
    const saved = localStorage.getItem('shoppingList');
    return saved ? JSON.parse(saved) : null;
  });

  const [visibleRecipes, setVisibleRecipes] = useState(() => {
    const saved = localStorage.getItem('visibleRecipes');
    return saved ? JSON.parse(saved) : [];
  });

  useEffect(() => {
    localStorage.setItem('people', people);
    localStorage.setItem('budget', budget);
    localStorage.setItem('selectedDays', JSON.stringify(selectedDays));
    localStorage.setItem('mealSelections', JSON.stringify(mealSelections));
    localStorage.setItem('results', JSON.stringify(results));
    localStorage.setItem('shoppingList', JSON.stringify(shoppingList));
    localStorage.setItem('visibleRecipes', JSON.stringify(visibleRecipes));
  }, [people, budget, selectedDays, mealSelections, results, shoppingList, visibleRecipes]);




  const toggleDay = (day) => {
    setSelectedDays(prev =>
      prev.includes(day) ? prev.filter(d => d !== day) : [...prev, day]
    );
  };

  const toggleMealType = (day, type) => {
    setMealSelections(prev => {
      const dayMeals = prev[day] || [];
      const updatedMeals = dayMeals.includes(type)
        ? dayMeals.filter(m => m !== type)
        : [...dayMeals, type];
      return { ...prev, [day]: updatedMeals };
    });
  };

  const handleGenerate = async () => {
    try {
      const res = await axios.post('/api/plan/week', {
        budget,
        people,
        weekdays: selectedDays,
        mealTypesByDay: mealSelections
      });
      setResults(res.data);

      const shopRes = await axios.post('/api/plan/shoppinglist', {
        weeklyResults: res.data,
        people
      });
      setShoppingList(shopRes.data);
    } catch (err) {
      console.error(err);
      alert('Viga planeerimisel');
    }
  };

  const regenerateDay = async (day) => {
    try {
      const res = await axios.post('/api/plan/day', {
        budget,
        people,
        mealTypes: mealSelections[day] || []
      });

      setResults(prev => ({
        ...prev,
        [day]: res.data
      }));

      const shopRes = await axios.post('/api/plan/shoppinglist', {
        weeklyResults: {
          ...results,
          [day]: res.data
        },
        people
      });
      setShoppingList(shopRes.data);
    } catch (err) {
      console.error(err);
      alert("Viga päeva uuendamisel");
    }
  };

  const regenerateMeal = async (day, mealType) => {
    try {
      const res = await axios.post('/api/plan/day', {
        budget: budget / allMealTypes.length,
        people,
        mealTypes: [mealType]
      });

      setResults(prev => ({
        ...prev,
        [day]: {
          ...prev[day],
          [mealType]: res.data[mealType]
        }
      }));

      const shopRes = await axios.post('/api/plan/shoppinglist', {
        weeklyResults: {
          ...results,
          [day]: {
            ...results[day],
            [mealType]: res.data[mealType]
          }
        },
        people
      });
      setShoppingList(shopRes.data);
    } catch (err) {
      console.error(err);
      alert("Viga toidukorra uuendamisel");
    }
  };

  const toggleRecipe = (day, mealType) => {
    const key = `${day}-${mealType}`;
    setVisibleRecipes(prev =>
      prev.includes(key) ? prev.filter(k => k !== key) : [...prev, key]
    );
  };

  const removeItem = (day, itemName) => {
    setShoppingList(prev => {
      const updated = { ...prev };
      if (updated[day]) {
        delete updated[day][itemName];
      }
      return updated;
    });
  };

  const evaluateCart = async () => {
    try {
      const normalizedCart = {};
      for (const day in shoppingList) {
        normalizedCart[day] = {};
        for (const name in shoppingList[day]) {
          const item = shoppingList[day][name];
          normalizedCart[day][name] = {
            name: name,
            quantity: item.quantity,
            unit: item.unit
          };
        }
      }

      const res = await axios.post(`/api/cart/evaluate?people=${people}&budget=${budget}`, normalizedCart);
      setComparisonData(res.data); // ← Salvestame siia
    } catch (err) {
      console.error(err);
      alert("Viga ostukorvi hindamisel");
    }
  };





  return (
    <div className="container py-4">
      <h1 className="mb-4 text-center">Ostukorvi Planeerija</h1>

      <div className="card mb-4">
        <div className="card-body">
          <div className="row g-3 mb-3">
            <div className="col-md-6">
              <label className="form-label">Inimeste arv:</label>
              <input type="number" className="form-control" value={people} onChange={e => setPeople(+e.target.value)} />
            </div>
            <div className="col-md-6">
              <label className="form-label">Eelarve (€):</label>
              <input type="number" className="form-control" value={budget} onChange={e => setBudget(+e.target.value)} />
            </div>
          </div>

          <div className="form-section mt-3">
            <p className="fw-semibold">Vali nädalapäevad ja soovitud söögikorrad:</p>
            <div className="d-flex flex-column gap-2">
              {allWeekdays.map((day, index) => (
                <div key={index}>
                  <div className="form-check">
                    <input
                      className="form-check-input"
                      type="checkbox"
                      id={`day-${index}`}
                      checked={selectedDays.includes(day)}
                      onChange={() => toggleDay(day)}
                    />
                    <label className="form-check-label fw-semibold ms-2" htmlFor={`day-${index}`}>
                      {day}
                    </label>
                  </div>

                  {selectedDays.includes(day) && (
                    <div className="ms-4 mt-1 mb-3">
                      {allMealTypes.map((type, i) => (
                        <div className="form-check form-check-inline" key={i}>
                          <input
                            className="form-check-input"
                            type="checkbox"
                            id={`${day}-${type}`}
                            checked={mealSelections[day]?.includes(type) || false}
                            onChange={() => toggleMealType(day, type)}
                          />
                          <label className="form-check-label" htmlFor={`${day}-${type}`}>
                            {type}
                          </label>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>



          <button className="btn btn-primary mt-3" onClick={handleGenerate}>Planeeri menüü</button>
        </div>
      </div>

      {results && (
        <div className="results mt-4">
          {allWeekdays.filter(day => results[day]).map(day => (
            <div key={day} className="card mb-3">
              <div className="card-header fw-bold">{day}</div>
              <div className="card-body p-3">
                {Object.entries(results[day]).map(([mealType, recipe], index) => {
                  const key = `${day}-${mealType}`;
                  const isVisible = visibleRecipes.includes(key);
                  return (
                    <div key={mealType} className="mb-3 border-top pt-2">
                      <div className="d-flex justify-content-between align-items-center mb-1">
                        <span className="fw-semibold">{mealType}:</span>
                        <div>
                          <button className="btn btn-sm btn-outline-primary me-2" onClick={() => regenerateMeal(day, mealType)}>Uuenda</button>
                          <button className="btn btn-sm btn-info text-white" onClick={() => toggleRecipe(day, mealType)}>
                            {isVisible ? 'Peida' : 'Vaata retsepti'}
                          </button>
                        </div>
                      </div>
                      <div className="ms-3">{recipe.name}</div>
                      {isVisible && (
                        <div className="mt-2 ms-3">
                          <p><strong>Koostisosad:</strong></p>
                          <ul>
                            {recipe.ingredients.map((ing, idx) => (
                              <li key={idx}>
                                {ing.quantity > 0.01
                                  ? `${ing.name}: ${(ing.quantity * people).toFixed(2)} ${ing.unit}`
                                  : ing.name}
                              </li>
                            ))}
                          </ul>
                          <p><strong>Juhised:</strong> {recipe.instructions}</p>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>
          ))}
        </div>
      )}

      {shoppingList && (
        <div className="card">
          <div className="card-body">
            <h2 className="card-title">Ostunimekiri</h2>
            <button className="btn btn-success mb-3" onClick={evaluateCart}>VÕRDLE HINDA POODIDES 🛒</button>
            {Object.entries(shoppingList).filter(([_, items]) => Object.keys(items).length > 0).map(([day, items]) => (
              <div key={day} className="mb-3">
                <h5>{day}</h5>
                <ul className="list-group">
                  {Object.entries(items).map(([name, item]) => (
                    <li key={name} className="list-group-item d-flex justify-content-between align-items-center">
                      {item.quantity > 0.01 ? `${name}: ${item.quantity.toFixed(2)} ${item.unit}` : name}
                      <button onClick={() => removeItem(day, name)} className="btn btn-light btn-sm text-white">✖</button>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </div>
      )}

      {comparisonData && (
        <div className="mt-4">
          <div className="card">
            <div className="card-body">
              <h4 className="card-title">Hinnavõrdlus</h4>
              <p className="card-text"><strong>Kõige odavam pood:</strong> {comparisonData.recommendedStore}</p>
              <p className="card-text"><strong>Ülejäänud eelarve:</strong> €{comparisonData.leftover.toFixed(2)}</p>

              {Object.entries(comparisonData.comparison).map(([store, info]) => (
                <div className="mt-3" key={store}>
                  <h5>{store}</h5>
                  <ul className="list-group">
                    <li className="list-group-item">Retseptikoguste hind: €{info.totalCost.toFixed(2)}</li>
                    <li className="list-group-item">Kogu ostukorvi hind: €{info.fullCost.toFixed(2)}</li>
                    {info.missingIngredients.length > 0 && (
                      <li className="list-group-item text-danger">
                        Võrdlusest puudu olevad tooted: {info.missingIngredients.join(', ')}
                      </li>
                    )}
                  </ul>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

    </div>
  );
}

export default App;
