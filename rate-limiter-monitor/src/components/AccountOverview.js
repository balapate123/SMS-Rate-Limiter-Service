import React, { useState, useEffect } from 'react';
import axios from 'axios';
import '../styles/AccountOverview.css';
import config from '../config';

const AccountOverview = () => {
    const [accounts, setAccounts] = useState([]);
    const expirationTimeInSeconds = 60;

    useEffect(() => {
        const fetchAccounts = async () => {
            try {
                // Fetch list of account IDs
                const accountResponse = await axios.get(`${config.apiBaseUrl}/accounts`);
                const accountIds = accountResponse.data;

                // Fetch details for each account ID
                const accountDetails = await Promise.all(
                    accountIds.map(async (accountId) => {
                        const accountData = await axios.get(`${config.apiBaseUrl}/${accountId}`);
                        return accountData.data;
                    })
                );

                setAccounts(accountDetails);
            } catch (error) {
                console.error('Error fetching accounts:', error);
            }
        };

        fetchAccounts();

        // Auto-refresh every 2 seconds
        const interval = setInterval(fetchAccounts, 2000); // Refresh every 2 seconds
        return () => clearInterval(interval); 
    }, []);

    const calculateMessagesPerSecond = (messageCount) => {
        return expirationTimeInSeconds > 0 ? (messageCount / expirationTimeInSeconds).toFixed(2) : "0.00";
    };

    return (
        <div className="container">
            <h1 className="header">Rate Limiter Monitoring</h1>
            <div className="account-container">
                <h2>Accounts Overview</h2>
                {accounts.map((account) => (
                    <div key={account.accountId} className="account-card">
                        <h3 className="account-title">Account ID: {account.accountId}</h3>
                        <p className="account-info">Messages per second: {calculateMessagesPerSecond(account.messageCount)}</p>
                        <p className="account-info">Messages sent: {account.messageCount}/{account.maxMessagesAllowed}</p>
                        <div className="phone-numbers-section">
                            <h4>Phone Numbers:</h4>
                            <ul className="phone-numbers">
                                {account.phoneNumbers.map((phoneNumber) => (
                                    <li key={phoneNumber} className="phone-number">
                                        {phoneNumber}
                                    </li>
                                ))}
                            </ul>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
};

export default AccountOverview;
